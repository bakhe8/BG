using System.Globalization;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Models.Requests;
using BG.Application.Operations;
using BG.Application.ReferenceData;
using BG.Application.Requests;
using BG.Application.Approvals;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.Application.Services;

internal sealed class RequestWorkspaceService : IRequestWorkspaceService
{
    private static readonly string[] RequestPermissionKeys =
    [
        "requests.view",
        "requests.create"
    ];

    private readonly IRequestWorkspaceRepository _repository;
    private readonly IWorkflowTemplateService _workflowTemplateService;
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;

    public RequestWorkspaceService(
        IRequestWorkspaceRepository repository,
        IWorkflowTemplateService workflowTemplateService,
        IWorkflowDefinitionRepository workflowDefinitionRepository)
    {
        _repository = repository;
        _workflowTemplateService = workflowTemplateService;
        _workflowDefinitionRepository = workflowDefinitionRepository;
    }

    public async Task<RequestWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? requestedActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var actors = await _repository.ListRequestActorsAsync(cancellationToken);
        var normalizedPageNumber = WorkspacePaging.NormalizePageNumber(pageNumber);

        if (actors.Count == 0)
        {
            return new RequestWorkspaceSnapshotDto(
                null,
                [],
                [],
                new PageInfoDto(1, WorkspacePaging.DefaultPageSize, 0),
                await _workflowTemplateService.GetTemplatesAsync(cancellationToken),
                false,
                "RequestsWorkspace_NoEligibleActor");
        }

        var activeActor = requestedActorId.HasValue
            ? actors.FirstOrDefault(actor => actor.Id == requestedActorId.Value)
            : actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        activeActor ??= actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        var ownedRequests = await _repository.ListOwnedRequestsAsync(
            activeActor.Id,
            normalizedPageNumber,
            WorkspacePaging.DefaultPageSize,
            cancellationToken);

        return new RequestWorkspaceSnapshotDto(
            MapActor(activeActor),
            actors
                .OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(MapActor)
                .ToArray(),
            ownedRequests.Items
                .Select(MapRequest)
                .ToArray(),
            ownedRequests.PageInfo,
            await _workflowTemplateService.GetTemplatesAsync(cancellationToken),
            true,
            "RequestsWorkspace_ActorIsolatedNotice");
    }

    public async Task<RequestWorkflowTemplateDto?> GetWorkflowTemplateAsync(
        string? guaranteeNumber,
        GuaranteeRequestType requestType,
        CancellationToken cancellationToken = default)
    {
        GuaranteeCategory? guaranteeCategory = null;

        if (!string.IsNullOrWhiteSpace(guaranteeNumber))
        {
            var guarantee = await _repository.GetGuaranteeByNumberAsync(guaranteeNumber.Trim(), cancellationToken);

            if (guarantee is null)
            {
                if (requestType == GuaranteeRequestType.Release)
                {
                    return null;
                }
            }
            else
            {
                guaranteeCategory = guarantee.Category;
            }
        }
        else if (requestType == GuaranteeRequestType.Release)
        {
            return null;
        }

        return await _workflowTemplateService.GetTemplateAsync(requestType, guaranteeCategory, cancellationToken);
    }

    public async Task<OperationResult<SubmitGuaranteeRequestReceiptDto>> SubmitRequestForApprovalAsync(
        Guid requestedByUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.UserContextRequired);
        }

        var actor = await _repository.GetRequestActorByIdAsync(requestedByUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.UserContextInvalid);
        }

        var request = await _repository.GetOwnedRequestByIdAsync(requestId, requestedByUserId, cancellationToken);
        if (request is null)
        {
            return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.GuaranteeNotFound);
        }

        if (request.Status is not GuaranteeRequestStatus.Draft and not GuaranteeRequestStatus.Returned)
        {
            return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.RequestValidationFailed);
        }

        var definition = await ResolveWorkflowDefinitionAsync(
            request.RequestType,
            request.Guarantee.Category,
            cancellationToken);

        if (definition is null || !definition.IsOperationallyReady)
        {
            return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure("RequestsWorkspace_NoWorkflowTemplate");
        }

        RequestApprovalProcess approvalProcess;

        if (request.ApprovalProcess is null)
        {
            var submittedAtUtc = DateTimeOffset.UtcNow;
            approvalProcess = new RequestApprovalProcess(
                request.Id,
                definition.Id,
                submittedAtUtc,
                definition.FinalSignatureDelegationPolicy,
                definition.DelegationAmountThreshold);

            foreach (var stage in definition.Stages.OrderBy(stage => stage.Sequence))
            {
                approvalProcess.AddStage(
                    stage.RoleId,
                    stage.TitleResourceKey,
                    stage.SummaryResourceKey,
                    string.IsNullOrWhiteSpace(stage.CustomTitle) && string.IsNullOrWhiteSpace(stage.TitleResourceKey) ? stage.Role?.Name : stage.CustomTitle,
                    string.IsNullOrWhiteSpace(stage.CustomSummary) && string.IsNullOrWhiteSpace(stage.SummaryResourceKey) ? stage.Role?.Description : stage.CustomSummary,
                    stage.RequiresLetterSignature,
                    stage.DelegationPolicy);
            }

            try
            {
                approvalProcess.Start();
            }
            catch (InvalidOperationException)
            {
                return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure("RequestsWorkspace_NoWorkflowTemplate");
            }
            request.SubmitForApproval(approvalProcess);
            var approvalCurrentStage = approvalProcess.GetCurrentStage();
            request.Guarantee.RecordRequestSubmittedForApproval(
                request.Id,
                submittedAtUtc,
                actor.Id,
                actor.DisplayName,
                approvalCurrentStage?.TitleText ?? approvalCurrentStage?.TitleResourceKey ?? approvalCurrentStage?.Role?.Name);
            _repository.TrackNewApprovalProcessGraph(approvalProcess);
            _repository.TrackLedgerEvents(
                request.Guarantee.Events.Where(ledgerEntry =>
                    ledgerEntry.GuaranteeRequestId == request.Id &&
                    ledgerEntry.EventType == GuaranteeEventType.RequestSubmittedForApproval &&
                    ledgerEntry.OccurredAtUtc == submittedAtUtc));
        }
        else
        {
            approvalProcess = request.ApprovalProcess;
            var resubmittedAtUtc = DateTimeOffset.UtcNow;
            try
            {
                approvalProcess.ResetForResubmission(resubmittedAtUtc);
            }
            catch (InvalidOperationException)
            {
                return OperationResult<SubmitGuaranteeRequestReceiptDto>.Failure("RequestsWorkspace_NoWorkflowTemplate");
            }
            request.SubmitForApproval(approvalProcess);
            var approvalCurrentStage = approvalProcess.GetCurrentStage();
            request.Guarantee.RecordRequestSubmittedForApproval(
                request.Id,
                resubmittedAtUtc,
                actor.Id,
                actor.DisplayName,
                approvalCurrentStage?.TitleText ?? approvalCurrentStage?.TitleResourceKey ?? approvalCurrentStage?.Role?.Name);
            _repository.TrackLedgerEvents(
                request.Guarantee.Events.Where(ledgerEntry =>
                    ledgerEntry.GuaranteeRequestId == request.Id &&
                    ledgerEntry.EventType == GuaranteeEventType.RequestSubmittedForApproval &&
                    ledgerEntry.OccurredAtUtc == resubmittedAtUtc));
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var currentStage = approvalProcess.GetCurrentStage();

        return OperationResult<SubmitGuaranteeRequestReceiptDto>.Success(
            new SubmitGuaranteeRequestReceiptDto(
                request.Id,
                currentStage?.TitleResourceKey,
                currentStage?.TitleText,
                currentStage?.Role?.Name));
    }

    public async Task<OperationResult<CreateGuaranteeRequestReceiptDto>> CreateRequestAsync(
        CreateGuaranteeRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.RequestedByUserId == Guid.Empty)
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.UserContextRequired);
        }

        var actor = await _repository.GetRequestActorByIdAsync(command.RequestedByUserId, cancellationToken);

        if (actor is null)
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.UserContextInvalid);
        }

        if (string.IsNullOrWhiteSpace(command.GuaranteeNumber))
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.GuaranteeNumberRequired);
        }

        var guaranteeNumber = command.GuaranteeNumber.Trim();
        var guarantee = await _repository.GetGuaranteeByNumberAsync(guaranteeNumber, cancellationToken);

        if (guarantee is null)
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.GuaranteeNotFound);
        }

        if (guarantee.Requests.Any(request =>
                request.IsOwnedBy(command.RequestedByUserId) &&
                request.RequestType == command.RequestType &&
                request.Status != GuaranteeRequestStatus.Completed &&
                request.Status != GuaranteeRequestStatus.Cancelled &&
                request.Status != GuaranteeRequestStatus.Rejected))
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.DuplicateOpenRequest);
        }

        var requestedAmount = ParseRequestedAmount(command.RequestType, command.RequestedAmount, out var amountErrorCode);
        if (amountErrorCode is not null)
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(amountErrorCode);
        }

        var requestedExpiryDate = ParseRequestedExpiryDate(command.RequestType, command.RequestedExpiryDate, out var expiryErrorCode);
        if (expiryErrorCode is not null)
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(expiryErrorCode);
        }

        try
        {
            var request = guarantee.CreateRequest(
                command.RequestedByUserId,
                command.RequestType,
                requestedAmount,
                requestedExpiryDate,
                command.Notes,
                DateTimeOffset.UtcNow,
                actor.DisplayName,
                GuaranteeRequestChannel.RequestWorkspace);

            var linkedAtUtc = DateTimeOffset.UtcNow;
            foreach (var document in guarantee.Documents
                         .Where(document => document.DocumentType is GuaranteeDocumentType.GuaranteeInstrument or GuaranteeDocumentType.SupportingDocument)
                         .OrderBy(document => document.CapturedAtUtc))
            {
                guarantee.AttachDocumentToRequest(
                    request.Id,
                    document.Id,
                    linkedAtUtc,
                    actor.Id,
                    actor.DisplayName);
            }

            _repository.TrackCreatedRequestGraph(request);
            _repository.TrackLedgerEvents(guarantee.Events.Where(ledgerEntry => ledgerEntry.GuaranteeRequestId == request.Id));
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<CreateGuaranteeRequestReceiptDto>.Success(
                new CreateGuaranteeRequestReceiptDto(
                    request.Id,
                    guarantee.GuaranteeNumber,
                    GuaranteeResourceCatalog.GetRequestTypeResourceKey(command.RequestType)));
        }
        catch (InvalidOperationException)
        {
            return OperationResult<CreateGuaranteeRequestReceiptDto>.Failure(RequestErrorCodes.RequestValidationFailed);
        }
    }

    private static decimal? ParseRequestedAmount(
        GuaranteeRequestType requestType,
        string? value,
        out string? errorCode)
    {
        errorCode = null;

        var requiresAmount = GuaranteeResourceCatalog.RequiresRequestedAmount(requestType);

        if (!requiresAmount)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errorCode = RequestErrorCodes.RequestedAmountRequired;
            return null;
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) &&
            !decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out amount))
        {
            errorCode = RequestErrorCodes.RequestedAmountRequired;
            return null;
        }

        return amount;
    }

    private static DateOnly? ParseRequestedExpiryDate(
        GuaranteeRequestType requestType,
        string? value,
        out string? errorCode)
    {
        errorCode = null;

        if (!GuaranteeResourceCatalog.RequiresRequestedExpiryDate(requestType))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errorCode = RequestErrorCodes.RequestedExpiryDateRequired;
            return null;
        }

        if (!DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
            !DateOnly.TryParse(value.Trim(), out date))
        {
            errorCode = RequestErrorCodes.RequestedExpiryDateRequired;
            return null;
        }

        return date;
    }

    private static RequestActorSummaryDto MapActor(BG.Domain.Identity.User user)
    {
        return new RequestActorSummaryDto(user.Id, user.Username, user.DisplayName);
    }

    private static RequestSummaryDto MapRequest(RequestListItemReadModel request)
    {
        var ledgerEntries = request.LedgerEntries
            .OrderByDescending(ledgerEntry => ledgerEntry.OccurredAtUtc)
            .Select(ledgerEntry => new RequestLedgerEntryDto(
                ledgerEntry.Id,
                ledgerEntry.OccurredAtUtc,
                ledgerEntry.ActorDisplayName,
                ledgerEntry.Summary,
                ledgerEntry.ApprovalStageLabel,
                ledgerEntry.ApprovalPolicyResourceKey,
                ledgerEntry.ApprovalResponsibleSignerDisplayName,
                ApprovalDecisionCatalog.TryGetExecutionModeResourceKey(ledgerEntry.ApprovalExecutionMode),
                ledgerEntry.DispatchStageResourceKey,
                ledgerEntry.DispatchMethodResourceKey,
                ledgerEntry.DispatchPolicyResourceKey,
                ledgerEntry.OperationsScenarioTitleResourceKey,
                ledgerEntry.OperationsLaneResourceKey,
                ledgerEntry.OperationsMatchConfidenceResourceKey,
                ledgerEntry.OperationsMatchScore,
                ledgerEntry.OperationsPolicyResourceKey))
            .ToArray();

        return new RequestSummaryDto(
            request.Id,
            request.GuaranteeNumber,
            GuaranteeResourceCatalog.GetGuaranteeCategoryResourceKey(request.GuaranteeCategory),
            GuaranteeResourceCatalog.GetRequestTypeResourceKey(request.RequestType),
            GuaranteeResourceCatalog.GetRequestStatusResourceKey(request.Status),
            request.RequestedAmount?.ToString("0.##", CultureInfo.InvariantCulture),
            request.RequestedExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            request.Notes,
            request.CreatedAtUtc,
            request.CorrespondenceCount,
            request.CurrentStageTitleResourceKey,
            request.CurrentStageTitle,
            request.CurrentStageRoleName,
            request.Status is GuaranteeRequestStatus.Draft or GuaranteeRequestStatus.Returned,
            MapLastDecision(request),
            request.LastDecisionNote,
            ledgerEntries);
    }

    private static string? MapLastDecision(RequestListItemReadModel request)
    {
        if (request.Status is GuaranteeRequestStatus.Draft)
        {
            return null;
        }

        return ApprovalDecisionCatalog.TryGetResourceKeyForRequestStatus(request.Status);
    }

    private async Task<RequestWorkflowDefinition?> ResolveWorkflowDefinitionAsync(
        GuaranteeRequestType requestType,
        GuaranteeCategory guaranteeCategory,
        CancellationToken cancellationToken)
    {
        var definition = await _workflowDefinitionRepository.GetDefinitionAsync(requestType, guaranteeCategory, cancellationToken);

        if (definition is not null && definition.IsActive && definition.IsOperationallyReady)
        {
            return definition;
        }

        var fallback = await _workflowDefinitionRepository.GetDefinitionAsync(requestType, guaranteeCategory: null, cancellationToken);
        return fallback is not null && fallback.IsActive && fallback.IsOperationallyReady
            ? fallback
            : null;
    }
}

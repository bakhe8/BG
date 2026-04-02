using BG.Application.Approvals;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Approvals;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace BG.Application.Services;

internal sealed class ApprovalQueueService : IApprovalQueueService
{
    private readonly IApprovalQueueRepository _repository;
    private readonly IIntakeDocumentStore _documentStore;
    private readonly ApprovalGovernanceOptions _governanceOptions;

    public ApprovalQueueService(
        IApprovalQueueRepository repository,
        IIntakeDocumentStore documentStore,
        IOptions<ApprovalGovernanceOptions> governanceOptions)
    {
        _repository = repository;
        _documentStore = documentStore;
        _governanceOptions = governanceOptions.Value;
    }

    public async Task<DocumentContentResult?> GetDocumentContentAsync(Guid requestId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetRequestDocumentAsync(requestId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var stream = _documentStore.GetDocumentContent(document.StoragePath);
        var contentType = ResolveContentType(document.FileName);

        return new DocumentContentResult(document.FileName, stream, contentType);
    }

    private static string ResolveContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tiff" or ".tif" => "image/tiff",
            _ => "application/octet-stream"
        };
    }

    public async Task<ApprovalQueueSnapshotDto> GetWorkspaceAsync(
        Guid? approverActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = WorkspacePaging.NormalizePageNumber(pageNumber);
        var actorContext = await ResolveActorContextAsync(approverActorId, cancellationToken);

        if (!actorContext.HasEligibleActor)
        {
            return new ApprovalQueueSnapshotDto(
                null,
                [],
                [],
                new PageInfoDto(1, WorkspacePaging.DefaultPageSize, 0),
                false,
                "ApprovalQueue_NoEligibleActor");
        }

        var activeActor = actorContext.ActiveActor!;
        var items = await _repository.ListActionableRequestsAsync(
            actorContext.ActionableRoleIds,
            normalizedPageNumber,
            WorkspacePaging.DefaultPageSize,
            cancellationToken);

        return new ApprovalQueueSnapshotDto(
            actorContext.ActiveActorSummary,
            actorContext.AvailableActorSummaries,
            items.Items
                .Select(request => MapItem(request, activeActor.Id, actorContext.DirectRoleIds, actorContext.DelegationByRoleId))
                .ToArray(),
            items.PageInfo,
            true,
            "ApprovalQueue_ActorScopedNotice");
    }

    public async Task<ApprovalRequestDossierSnapshotDto> GetDossierAsync(
        Guid? approverActorId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var actorContext = await ResolveActorContextAsync(approverActorId, cancellationToken);

        if (!actorContext.HasEligibleActor)
        {
            return new ApprovalRequestDossierSnapshotDto(
                null,
                [],
                null,
                false,
                "ApprovalQueue_NoEligibleActor",
                null);
        }

        var activeActor = actorContext.ActiveActor!;

        var request = await _repository.GetActionableRequestAsync(
            requestId,
            actorContext.ActionableRoleIds,
            cancellationToken);

        return new ApprovalRequestDossierSnapshotDto(
            actorContext.ActiveActorSummary,
            actorContext.AvailableActorSummaries,
            request is null
                ? null
                : MapItem(request, activeActor.Id, actorContext.DirectRoleIds, actorContext.DelegationByRoleId),
            true,
            "ApprovalQueue_ActorScopedNotice",
            request is null ? "ApprovalDossier_RequestNotAvailable" : null);
    }

    public Task<OperationResult<ApprovalDecisionReceiptDto>> ApproveAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default)
    {
        return ApplyDecisionAsync(
            command,
            (process, actedAtUtc, actedOnBehalfOfUserId, approvalDelegationId) => process.ApproveCurrentStage(
                command.ApproverUserId,
                actedAtUtc,
                command.Note,
                actedOnBehalfOfUserId,
                approvalDelegationId),
            request => request.MarkApprovedForDispatch(),
            ApprovalDecisionOutcome.Approved,
            cancellationToken);
    }

    public Task<OperationResult<ApprovalDecisionReceiptDto>> ReturnAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default)
    {
        return ApplyDecisionAsync(
            command,
            (process, actedAtUtc, actedOnBehalfOfUserId, approvalDelegationId) =>
            {
                process.ReturnCurrentStage(
                    command.ApproverUserId,
                    actedAtUtc,
                    command.Note,
                    actedOnBehalfOfUserId,
                    approvalDelegationId);
                return false;
            },
            request => request.MarkReturnedFromApproval(),
            ApprovalDecisionOutcome.Returned,
            cancellationToken);
    }

    public Task<OperationResult<ApprovalDecisionReceiptDto>> RejectAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default)
    {
        return ApplyDecisionAsync(
            command,
            (process, actedAtUtc, actedOnBehalfOfUserId, approvalDelegationId) =>
            {
                process.RejectCurrentStage(
                    command.ApproverUserId,
                    actedAtUtc,
                    command.Note,
                    actedOnBehalfOfUserId,
                    approvalDelegationId);
                return false;
            },
            request => request.MarkRejectedByApproval(),
            ApprovalDecisionOutcome.Rejected,
            cancellationToken);
    }

    private async Task<OperationResult<ApprovalDecisionReceiptDto>> ApplyDecisionAsync(
        ApprovalDecisionCommand command,
        Func<BG.Domain.Workflow.RequestApprovalProcess, DateTimeOffset, Guid?, Guid?, bool> decision,
        Action<GuaranteeRequest> requestTransition,
        ApprovalDecisionOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (command.ApproverUserId == Guid.Empty)
        {
            return OperationResult<ApprovalDecisionReceiptDto>.Failure(ApprovalErrorCodes.ApproverContextRequired);
        }

        var actor = await _repository.GetApprovalActorByIdAsync(command.ApproverUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<ApprovalDecisionReceiptDto>.Failure(ApprovalErrorCodes.ApproverContextInvalid);
        }

        var request = await _repository.GetRequestForApprovalAsync(command.RequestId, cancellationToken);
        if (request?.ApprovalProcess is null)
        {
            return OperationResult<ApprovalDecisionReceiptDto>.Failure(ApprovalErrorCodes.RequestNotFound);
        }

        var actedAtUtc = DateTimeOffset.UtcNow;
        var actorRoleIds = actor.UserRoles
            .Select(userRole => userRole.RoleId)
            .Distinct()
            .ToHashSet();
        var activeDelegations = await _repository.ListActiveDelegationsAsync(actor.Id, actedAtUtc, cancellationToken);
        var effectiveRoleIds = actorRoleIds
            .Concat(activeDelegations.Select(delegation => delegation.RoleId))
            .Distinct()
            .ToArray();
        var currentStage = request.ApprovalProcess.GetCurrentStage();

        if (currentStage?.RoleId is null || !request.ApprovalProcess.IsActionableByAnyOf(effectiveRoleIds))
        {
            return OperationResult<ApprovalDecisionReceiptDto>.Failure(ApprovalErrorCodes.RequestNotActionable);
        }

        var delegation = actorRoleIds.Contains(currentStage.RoleId.Value)
            ? null
            : activeDelegations
                .Where(candidate => candidate.RoleId == currentStage.RoleId.Value)
                .OrderBy(candidate => candidate.StartsAtUtc)
                .FirstOrDefault();

        if (!actorRoleIds.Contains(currentStage.RoleId.Value) && delegation is null)
        {
            return OperationResult<ApprovalDecisionReceiptDto>.Failure(ApprovalErrorCodes.RequestNotActionable);
        }

        var governance = ApprovalGovernanceEvaluator.Evaluate(
            request.ApprovalProcess,
            request.RequestType,
            request.RequestedAmount ?? request.Guarantee.CurrentAmount,
            actor.Id,
            delegation?.DelegatorUserId ?? actor.Id,
            _governanceOptions);
        if (governance.IsDecisionBlocked)
        {
            return OperationResult<ApprovalDecisionReceiptDto>.Failure(ApprovalErrorCodes.GovernancePolicyBlocked);
        }

        var stageLabel = currentStage?.TitleText ?? currentStage?.TitleResourceKey ?? currentStage?.Role?.Name;
        var responsibleSignerDisplayName = delegation?.DelegatorUser.DisplayName ?? actor.DisplayName;
        var approvalExecutionMode = delegation is null
            ? ApprovalLedgerExecutionMode.Direct
            : ApprovalLedgerExecutionMode.Delegated;
        var approvalCompleted = decision(
            request.ApprovalProcess,
            actedAtUtc,
            delegation?.DelegatorUserId,
            delegation?.Id);
        if (approvalCompleted || ApprovalDecisionCatalog.RequiresImmediateRequestTransition(outcome))
        {
            requestTransition(request);
        }

        var ledgerEventType = ApprovalDecisionCatalog.GetLedgerEventType(outcome);

        request.Guarantee.RecordApprovalDecision(
            request.Id,
            ledgerEventType,
            actedAtUtc,
            actor.Id,
            actor.DisplayName,
            responsibleSignerDisplayName,
            stageLabel,
            governance.AppliedPolicyResourceKey,
            approvalExecutionMode,
            command.Note);

        await _repository.SaveChangesAsync(cancellationToken);

        return OperationResult<ApprovalDecisionReceiptDto>.Success(
            new ApprovalDecisionReceiptDto(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                ApprovalDecisionCatalog.GetResourceKey(outcome)));
    }

    private ApprovalQueueItemDto MapItem(
        ApprovalQueueItemReadModel request,
        Guid actorUserId,
        ISet<Guid> directRoleIds,
        IReadOnlyDictionary<Guid, BG.Domain.Workflow.ApprovalDelegation> delegationByRoleId)
    {
        BG.Domain.Workflow.ApprovalDelegation? delegation = null;
        if (request.CurrentStageRoleId is Guid roleId && !directRoleIds.Contains(roleId))
        {
            delegationByRoleId.TryGetValue(roleId, out delegation);
        }

        var governance = ApprovalGovernanceEvaluator.Evaluate(
            request,
            actorUserId,
            delegation?.DelegatorUserId ?? actorUserId,
            _governanceOptions);
        var priorSignatures = request.PriorSignatures
            .OrderBy(signature => signature.Sequence)
            .Select(signature => new ApprovalPriorSignatureDto(
                signature.StageId,
                signature.Sequence,
                signature.StageTitleResourceKey,
                signature.StageTitle,
                signature.StageRoleName,
                signature.ActedAtUtc,
                signature.ActorDisplayName,
                signature.ResponsibleSignerDisplayName,
                signature.ResponsibleSignerUserId != signature.ActorUserId))
            .ToArray();

        var attachments = request.Attachments
            .Select(link => new ApprovalRequestAttachmentDto(
                link.Id,
                link.GuaranteeDocumentId,
                link.FileName,
                GuaranteeResourceCatalog.GetDocumentTypeResourceKey(link.DocumentType),
                link.LinkedAtUtc,
                link.LinkedByDisplayName,
                link.CapturedAtUtc,
                link.CapturedByDisplayName,
                GuaranteeResourceCatalog.GetCaptureChannelResourceKey(link.CaptureChannel),
                link.SourceSystemName,
                link.SourceReference,
                GuaranteeDocumentFormCatalog.ToSnapshot(
                    IntakeVerifiedDataParser.ResolveDocumentForm(
                        link.DocumentType,
                        link.VerifiedDataJson))))
            .ToArray();
        var timelineEntries = request.TimelineEntries
            .Select(ledgerEntry => new ApprovalRequestTimelineEntryDto(
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

        return new ApprovalQueueItemDto(
            request.RequestId,
            request.GuaranteeNumber,
            GuaranteeResourceCatalog.GetGuaranteeCategoryResourceKey(request.GuaranteeCategory),
            GuaranteeResourceCatalog.GetRequestTypeResourceKey(request.RequestType),
            GuaranteeResourceCatalog.GetRequestChannelResourceKey(request.RequestChannel),
            GuaranteeResourceCatalog.GetRequestStatusResourceKey(request.Status),
            request.RequesterDisplayName,
            request.CreatedAtUtc,
            request.SubmittedAtUtc,
            request.RequestedAmount?.ToString("0.##", CultureInfo.InvariantCulture),
            request.RequestedExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            request.Notes,
            request.CurrentStageTitleResourceKey,
            request.CurrentStageTitle,
            request.CurrentStageRoleName,
            delegation?.DelegatorUser.DisplayName,
            delegation?.EndsAtUtc,
            delegation?.Reason,
            request.RequiresLetterSignature,
            governance.ToDto(),
            priorSignatures,
            attachments,
            timelineEntries);
    }

    private async Task<ApprovalActorContext> ResolveActorContextAsync(Guid? approverActorId, CancellationToken cancellationToken)
    {
        var actors = await _repository.ListApprovalActorsAsync(cancellationToken);

        if (actors.Count == 0)
        {
            return ApprovalActorContext.Empty;
        }

        var activeActor = approverActorId.HasValue
            ? actors.FirstOrDefault(actor => actor.Id == approverActorId.Value)
            : actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        activeActor ??= actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        var effectiveAtUtc = DateTimeOffset.UtcNow;
        var directRoleIds = activeActor.UserRoles
            .Select(userRole => userRole.RoleId)
            .Distinct()
            .ToHashSet();
        var activeDelegations = await _repository.ListActiveDelegationsAsync(activeActor.Id, effectiveAtUtc, cancellationToken);
        var actionableRoleIds = directRoleIds
            .Concat(activeDelegations.Select(delegation => delegation.RoleId))
            .Distinct()
            .ToArray();
        var delegationByRoleId = activeDelegations
            .GroupBy(delegation => delegation.RoleId)
            .ToDictionary(group => group.Key, group => group.OrderBy(delegation => delegation.StartsAtUtc).First());

        return new ApprovalActorContext(
            activeActor,
            new ApprovalActorSummaryDto(activeActor.Id, activeActor.Username, activeActor.DisplayName),
            actors
                .OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(actor => new ApprovalActorSummaryDto(actor.Id, actor.Username, actor.DisplayName))
                .ToArray(),
            directRoleIds,
            actionableRoleIds,
            delegationByRoleId);
    }

    private sealed record ApprovalActorContext(
        BG.Domain.Identity.User? ActiveActor,
        ApprovalActorSummaryDto? ActiveActorSummary,
        IReadOnlyList<ApprovalActorSummaryDto> AvailableActorSummaries,
        ISet<Guid> DirectRoleIds,
        IReadOnlyList<Guid> ActionableRoleIds,
        IReadOnlyDictionary<Guid, BG.Domain.Workflow.ApprovalDelegation> DelegationByRoleId)
    {
        public static ApprovalActorContext Empty { get; } = new(
            null,
            null,
            [],
            new HashSet<Guid>(),
            Array.Empty<Guid>(),
            new Dictionary<Guid, BG.Domain.Workflow.ApprovalDelegation>());

        public bool HasEligibleActor => ActiveActor is not null && ActiveActorSummary is not null;
    }
}

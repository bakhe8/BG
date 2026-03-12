using System.Globalization;
using System.Text.Json;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Operations;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Domain.Operations;

namespace BG.Application.Services;

internal sealed class OperationsReviewQueueService : IOperationsReviewQueueService
{
    private readonly IOperationsReviewRepository _repository;
    private readonly IWorkflowTemplateService _workflowTemplateService;
    private readonly IOperationsReviewMatchingService _matchingService;

    public OperationsReviewQueueService(
        IOperationsReviewRepository repository,
        IWorkflowTemplateService workflowTemplateService,
        IOperationsReviewMatchingService matchingService)
    {
        _repository = repository;
        _workflowTemplateService = workflowTemplateService;
        _matchingService = matchingService;
    }

    public async Task<OperationsReviewQueueSnapshotDto> GetSnapshotAsync(
        Guid? operationsActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var actors = await _repository.ListOperationsActorsAsync(cancellationToken);
        var normalizedPageNumber = WorkspacePaging.NormalizePageNumber(pageNumber);

        if (actors.Count == 0)
        {
            return new OperationsReviewQueueSnapshotDto(
                null,
                [],
                [],
                new PageInfoDto(1, WorkspacePaging.DefaultPageSize, 0),
                await _workflowTemplateService.GetTemplatesAsync(cancellationToken),
                0,
                0,
                0,
                false,
                "OperationsQueue_NoEligibleActor");
        }

        var activeActor = operationsActorId.HasValue
            ? actors.FirstOrDefault(actor => actor.Id == operationsActorId.Value)
            : actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        activeActor ??= actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        var items = await _repository.ListOpenAsync(
            normalizedPageNumber,
            WorkspacePaging.DefaultPageSize,
            cancellationToken);
        var mappedItems = items.Items.Items
            .Select(MapItem)
            .ToArray();

        return new OperationsReviewQueueSnapshotDto(
            new OperationsActorSummaryDto(activeActor.Id, activeActor.Username, activeActor.DisplayName),
            actors
                .OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(actor => new OperationsActorSummaryDto(actor.Id, actor.Username, actor.DisplayName))
                .ToArray(),
            mappedItems,
            items.Items.PageInfo,
            await _workflowTemplateService.GetTemplatesAsync(cancellationToken),
            items.Counts.OpenItemsCount,
            items.Counts.PendingItemsCount,
            items.Counts.RoutedItemsCount,
            true,
            "OperationsQueue_ActorScopedNotice");
    }

    public async Task<OperationResult<ApplyBankResponseReceiptDto>> ApplyBankResponseAsync(
        ApplyBankResponseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OperationsActorUserId == Guid.Empty)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.OperationsActorRequired);
        }

        var actor = await _repository.GetOperationsActorByIdAsync(command.OperationsActorUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.OperationsActorInvalid);
        }

        if (command.RequestId == Guid.Empty)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.RequestRequired);
        }

        var item = await _repository.GetOpenItemByIdAsync(command.ReviewItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.ReviewItemNotFound);
        }

        if (item.GuaranteeCorrespondence is null)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.ReviewItemNotActionable);
        }

        var request = item.Guarantee.Requests.SingleOrDefault(candidate => candidate.Id == command.RequestId);
        if (request is null)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.RequestNotFound);
        }

        var itemReadModel = CreateReadModel(item);
        var suggestions = _matchingService.SuggestMatches(itemReadModel);
        var selectedSuggestion = suggestions.FirstOrDefault(suggestion => suggestion.RequestId == request.Id);

        var scenario = IntakeScenarioCatalog.Find(item.ScenarioKey);
        if (!IsCompatible(scenario, request.RequestType))
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.RequestNotCompatible);
        }

        var suggestedValues = ParseSuggestedValues(item.GuaranteeDocument.VerifiedDataJson);
        var note = Normalize(command.Note) ?? suggestedValues.StatusStatement;

        DateOnly? confirmedExpiryDate = null;
        decimal? confirmedAmount = null;
        var replacementGuaranteeNumber = Normalize(command.ReplacementGuaranteeNumber);

        switch (request.RequestType)
        {
            case GuaranteeRequestType.Extend:
                if (!TryParseDate(Normalize(command.ConfirmedExpiryDate) ?? suggestedValues.ConfirmedExpiryDate, out var parsedExpiryDate))
                {
                    return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.ConfirmedExpiryDateRequired);
                }

                confirmedExpiryDate = parsedExpiryDate;
                break;

            case GuaranteeRequestType.Reduce:
                if (!TryParseAmount(Normalize(command.ConfirmedAmount) ?? suggestedValues.ConfirmedAmount, out var parsedAmount))
                {
                    return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.ConfirmedAmountRequired);
                }

                confirmedAmount = parsedAmount;
                break;

            case GuaranteeRequestType.ReplaceWithReducedGuarantee:
                if (string.IsNullOrWhiteSpace(replacementGuaranteeNumber))
                {
                    return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.ReplacementGuaranteeNumberRequired);
                }
                break;

            case GuaranteeRequestType.Release:
            case GuaranteeRequestType.VerifyStatus:
                break;

            default:
                return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.ResponseTypeNotSupported);
        }

        try
        {
            var appliedAtUtc = DateTimeOffset.UtcNow;
            item.Guarantee.ApplyBankConfirmation(
                request.Id,
                item.GuaranteeCorrespondence.Id,
                appliedAtUtc,
                confirmedExpiryDate,
                confirmedAmount,
                replacementGuaranteeNumber,
                note,
                actor.Id,
                actor.DisplayName,
                scenario?.TitleResourceKey,
                OperationsReviewResourceCatalog.GetRecommendedLaneResourceKey(item.Category),
                selectedSuggestion?.ConfidenceResourceKey,
                selectedSuggestion?.Score,
                selectedSuggestion is null
                    ? "OperationsLedgerPolicy_ManualRequestSelectionApplied"
                    : "OperationsLedgerPolicy_MatchedSuggestionApplied");

            item.MarkCompleted(appliedAtUtc);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<ApplyBankResponseReceiptDto>.Failure(OperationsReviewErrorCodes.RequestNotCompatible);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return OperationResult<ApplyBankResponseReceiptDto>.Success(
            new ApplyBankResponseReceiptDto(
                item.Id,
                request.Id,
                item.Guarantee.GuaranteeNumber));
    }

    private OperationsReviewItemDto MapItem(OperationsReviewQueueItemReadModel item)
    {
        var scenario = IntakeScenarioCatalog.Find(item.ScenarioKey);
        var suggestions = _matchingService.SuggestMatches(item);
        var suggestedValues = ParseSuggestedValues(item.VerifiedDataJson);

        return new OperationsReviewItemDto(
            item.Id,
            item.GuaranteeNumber,
            item.ScenarioKey,
            scenario?.TitleResourceKey ?? "OperationsReviewScenario_Unknown",
            OperationsReviewResourceCatalog.GetCategoryResourceKey(item.Category),
            ResolveStatus(item.Status),
            OperationsReviewResourceCatalog.GetRecommendedLaneResourceKey(item.Category),
            item.FileName,
            item.BankReference,
            item.CreatedAtUtc,
            item.CapturedAtUtc,
            item.CapturedByDisplayName,
            GuaranteeResourceCatalog.GetCaptureChannelResourceKey(item.CaptureChannel),
            item.SourceSystemName,
            item.SourceReference,
            OperationsReviewMatchingService.SupportsMatching(item),
            suggestions,
            suggestedValues.ConfirmedExpiryDate,
            suggestedValues.ConfirmedAmount,
            suggestedValues.StatusStatement,
            scenario?.RequiresConfirmedExpiryDate ?? false,
            scenario?.RequiresConfirmedAmount ?? false);
    }

    private static OperationsReviewQueueItemReadModel CreateReadModel(OperationsReviewItem item)
    {
        return new OperationsReviewQueueItemReadModel(
            item.Id,
            item.Guarantee.Id,
            item.GuaranteeNumber,
            item.ScenarioKey,
            item.Category,
            item.Status,
            item.GuaranteeDocument.FileName,
            item.GuaranteeCorrespondence?.ReferenceNumber,
            item.CreatedAtUtc,
            item.GuaranteeDocument.CapturedAtUtc,
            item.GuaranteeDocument.CapturedByDisplayName,
            item.GuaranteeDocument.CaptureChannel,
            item.GuaranteeDocument.SourceSystemName,
            item.GuaranteeDocument.SourceReference,
            item.GuaranteeDocument.VerifiedDataJson,
            item.GuaranteeCorrespondence?.LetterDate,
            item.Guarantee.Requests
                .Select(request => new OperationsReviewRequestCandidateReadModel(
                    request.Id,
                    request.RequestType,
                    request.Status,
                    request.RequestedExpiryDate,
                    request.RequestedAmount,
                    request.SubmittedToBankAtUtc,
                    request.Correspondence
                        .Where(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)
                        .OrderByDescending(correspondence => correspondence.RegisteredAtUtc)
                        .Select(correspondence => correspondence.ReferenceNumber)
                        .FirstOrDefault()))
                .ToArray());
    }

    private static string ResolveStatus(OperationsReviewItemStatus status)
    {
        return status switch
        {
            OperationsReviewItemStatus.Pending => "OperationsReviewStatus_Pending",
            OperationsReviewItemStatus.Routed => "OperationsReviewStatus_Routed",
            OperationsReviewItemStatus.Completed => "OperationsReviewStatus_Completed",
            _ => "OperationsReviewStatus_Pending"
        };
    }

    private static bool IsCompatible(IntakeScenarioDefinition? scenario, GuaranteeRequestType requestType)
    {
        return scenario?.ExpectedRequestType == requestType;
    }

    private static SuggestedConfirmationValues ParseSuggestedValues(string? verifiedDataJson)
    {
        if (string.IsNullOrWhiteSpace(verifiedDataJson))
        {
            return new SuggestedConfirmationValues(null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(verifiedDataJson);
            var root = document.RootElement;

            return new SuggestedConfirmationValues(
                TryReadString(root, IntakeVerifiedDataKeys.NewExpiryDate),
                TryReadString(root, IntakeVerifiedDataKeys.Amount),
                TryReadString(root, IntakeVerifiedDataKeys.StatusStatement));
        }
        catch (JsonException)
        {
            return new SuggestedConfirmationValues(null, null, null);
        }
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? Normalize(property.GetString())
            : null;
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = default;
            return false;
        }

        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateOnly.TryParse(value, out date);
    }

    private static bool TryParseAmount(string? value, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            amount = default;
            return false;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
               || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record SuggestedConfirmationValues(
        string? ConfirmedExpiryDate,
        string? ConfirmedAmount,
        string? StatusStatement);
}

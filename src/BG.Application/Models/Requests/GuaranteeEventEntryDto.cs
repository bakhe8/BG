namespace BG.Application.Models.Requests;

public sealed record GuaranteeEventEntryDto(
    Guid Id,
    string EventTypeResourceKey,
    string EventIconKey,
    string Summary,
    string? ActorDisplayName,
    DateTimeOffset OccurredAtUtc,
    decimal? PreviousAmount,
    decimal? NewAmount,
    DateOnly? PreviousExpiryDate,
    DateOnly? NewExpiryDate,
    string? PreviousStatus,
    string? NewStatus,
    string? ApprovalStageLabel,
    string? ApprovalPolicyResourceKey,
    string? DispatchStageResourceKey,
    string? OperationsScenarioTitleResourceKey,
    Guid? GuaranteeRequestId);

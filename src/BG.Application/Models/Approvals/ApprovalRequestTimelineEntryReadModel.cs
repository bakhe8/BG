namespace BG.Application.Models.Approvals;

public sealed record ApprovalRequestTimelineEntryReadModel(
    Guid RequestId,
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string? ActorDisplayName,
    string Summary,
    string? ApprovalStageLabel = null,
    string? ApprovalPolicyResourceKey = null,
    string? ApprovalResponsibleSignerDisplayName = null,
    string? ApprovalExecutionMode = null,
    string? DispatchStageResourceKey = null,
    string? DispatchMethodResourceKey = null,
    string? DispatchPolicyResourceKey = null,
    string? OperationsScenarioTitleResourceKey = null,
    string? OperationsLaneResourceKey = null,
    string? OperationsMatchConfidenceResourceKey = null,
    int? OperationsMatchScore = null,
    string? OperationsPolicyResourceKey = null);

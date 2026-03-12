namespace BG.Application.Models.Approvals;

public sealed record ApprovalDecisionCommand(
    Guid ApproverUserId,
    Guid RequestId,
    string? Note);

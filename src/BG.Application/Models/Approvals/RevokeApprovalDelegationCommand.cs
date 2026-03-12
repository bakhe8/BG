namespace BG.Application.Models.Approvals;

public sealed record RevokeApprovalDelegationCommand(
    Guid DelegationId,
    string? Reason);

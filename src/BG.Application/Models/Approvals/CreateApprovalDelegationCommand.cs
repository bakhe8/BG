namespace BG.Application.Models.Approvals;

public sealed record CreateApprovalDelegationCommand(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    Guid RoleId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Reason);

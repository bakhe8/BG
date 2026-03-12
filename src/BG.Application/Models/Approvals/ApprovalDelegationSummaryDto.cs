namespace BG.Application.Models.Approvals;

public sealed record ApprovalDelegationSummaryDto(
    Guid Id,
    Guid DelegatorUserId,
    string DelegatorDisplayName,
    Guid DelegateUserId,
    string DelegateDisplayName,
    Guid RoleId,
    string RoleName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    string StatusResourceKey,
    DateTimeOffset? RevokedAtUtc,
    string? RevocationReason);

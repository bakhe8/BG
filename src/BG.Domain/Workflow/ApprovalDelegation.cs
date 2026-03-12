using BG.Domain.Identity;

namespace BG.Domain.Workflow;

public sealed class ApprovalDelegation
{
    public ApprovalDelegation(
        Guid delegatorUserId,
        Guid delegateUserId,
        Guid roleId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string? reason,
        DateTimeOffset createdAtUtc)
    {
        if (delegatorUserId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(delegatorUserId), "Delegator user is required.");
        }

        if (delegateUserId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(delegateUserId), "Delegate user is required.");
        }

        if (delegatorUserId == delegateUserId)
        {
            throw new InvalidOperationException("Delegator and delegate must be different users.");
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "Role is required.");
        }

        if (endsAtUtc <= startsAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAtUtc), "Delegation end must be later than the start.");
        }

        Id = Guid.NewGuid();
        DelegatorUserId = delegatorUserId;
        DelegateUserId = delegateUserId;
        RoleId = roleId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Reason = NormalizeOptional(reason, 512);
        CreatedAtUtc = createdAtUtc;
    }

    private ApprovalDelegation()
    {
    }

    public Guid Id { get; private set; }

    public Guid DelegatorUserId { get; private set; }

    public Guid DelegateUserId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public User DelegatorUser { get; internal set; } = default!;

    public User DelegateUser { get; internal set; } = default!;

    public Role Role { get; internal set; } = default!;

    public bool IsActiveAt(DateTimeOffset effectiveAtUtc)
    {
        return !RevokedAtUtc.HasValue &&
               StartsAtUtc <= effectiveAtUtc &&
               EndsAtUtc >= effectiveAtUtc;
    }

    public bool Overlaps(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        return !RevokedAtUtc.HasValue &&
               startsAtUtc <= EndsAtUtc &&
               endsAtUtc >= StartsAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc, string? revocationReason)
    {
        if (RevokedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Delegation was already revoked.");
        }

        RevokedAtUtc = revokedAtUtc;
        RevocationReason = NormalizeOptional(revocationReason, 512);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Maximum length is {maxLength} characters.");
        }

        return normalized;
    }
}

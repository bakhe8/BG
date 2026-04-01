namespace BG.Web.Security;

public sealed record LoginLockoutDecision(
    bool IsLockedOut,
    int FailureCount,
    DateTimeOffset? LockedUntilUtc);

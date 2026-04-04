namespace BG.Infrastructure.Identity;

internal sealed class LoginAttemptRecord
{
    public string TrackingKey { get; set; } = string.Empty;
    public int FailureCount { get; set; }
    public DateTimeOffset WindowExpiresAtUtc { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

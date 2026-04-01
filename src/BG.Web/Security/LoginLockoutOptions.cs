namespace BG.Web.Security;

public sealed class LoginLockoutOptions
{
    public const string SectionName = "Identity:LoginLockout";

    public int MaxFailedAttempts { get; set; } = 5;

    public int TrackingWindowMinutes { get; set; } = 15;

    public int LockoutMinutes { get; set; } = 15;
}

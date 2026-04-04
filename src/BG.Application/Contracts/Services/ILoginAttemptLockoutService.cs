using System.Net;

namespace BG.Application.Contracts.Services;

public interface ILoginAttemptLockoutService
{
    LoginLockoutDecision GetDecision(string? username, IPAddress? remoteIpAddress);

    LoginLockoutDecision RegisterFailure(string? username, IPAddress? remoteIpAddress);

    void Reset(string? username, IPAddress? remoteIpAddress);
}

public sealed record LoginLockoutDecision(
    bool IsLockedOut,
    int FailureCount,
    DateTimeOffset? LockedUntilUtc);

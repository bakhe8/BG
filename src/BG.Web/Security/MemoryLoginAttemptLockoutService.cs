using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BG.Web.Security;

public sealed class MemoryLoginAttemptLockoutService : ILoginAttemptLockoutService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<LoginLockoutOptions> _options;

    public MemoryLoginAttemptLockoutService(
        IMemoryCache memoryCache,
        IOptions<LoginLockoutOptions> options)
    {
        _memoryCache = memoryCache;
        _options = options;
    }

    public LoginLockoutDecision GetDecision(string? username, IPAddress? remoteIpAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var key = BuildKey(username, remoteIpAddress);
        if (!_memoryCache.TryGetValue<LoginAttemptState>(key, out var state) || state is null)
        {
            return new LoginLockoutDecision(false, 0, null);
        }

        if (state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value > now)
        {
            return new LoginLockoutDecision(true, state.FailureCount, state.LockedUntilUtc);
        }

        if (state.WindowExpiresAtUtc <= now)
        {
            _memoryCache.Remove(key);
            return new LoginLockoutDecision(false, 0, null);
        }

        return new LoginLockoutDecision(false, state.FailureCount, null);
    }

    public LoginLockoutDecision RegisterFailure(string? username, IPAddress? remoteIpAddress)
    {
        var options = Normalize(_options.Value);
        var key = BuildKey(username, remoteIpAddress);
        var now = DateTimeOffset.UtcNow;

        _memoryCache.TryGetValue<LoginAttemptState>(key, out var existingState);

        var failureCount = existingState is not null && existingState.WindowExpiresAtUtc > now
            ? existingState.FailureCount + 1
            : 1;

        var windowExpiresAtUtc = now.AddMinutes(options.TrackingWindowMinutes);
        DateTimeOffset? lockedUntilUtc = null;

        if (failureCount >= options.MaxFailedAttempts)
        {
            lockedUntilUtc = now.AddMinutes(options.LockoutMinutes);
        }

        var expiresAtUtc = lockedUntilUtc.HasValue && lockedUntilUtc.Value > windowExpiresAtUtc
            ? lockedUntilUtc.Value
            : windowExpiresAtUtc;

        _memoryCache.Set(
            key,
            new LoginAttemptState(failureCount, windowExpiresAtUtc, lockedUntilUtc),
            expiresAtUtc);

        return new LoginLockoutDecision(lockedUntilUtc.HasValue, failureCount, lockedUntilUtc);
    }

    public void Reset(string? username, IPAddress? remoteIpAddress)
    {
        _memoryCache.Remove(BuildKey(username, remoteIpAddress));
    }

    private static string BuildKey(string? username, IPAddress? remoteIpAddress)
    {
        var normalizedUsername = string.IsNullOrWhiteSpace(username)
            ? "(blank)"
            : username.Trim().ToUpperInvariant();

        var normalizedIpAddress = remoteIpAddress?.ToString() ?? "unknown";
        return $"login-lockout::{normalizedUsername}::{normalizedIpAddress}";
    }

    private static LoginLockoutOptions Normalize(LoginLockoutOptions options)
    {
        return new LoginLockoutOptions
        {
            MaxFailedAttempts = Math.Max(1, options.MaxFailedAttempts),
            TrackingWindowMinutes = Math.Max(1, options.TrackingWindowMinutes),
            LockoutMinutes = Math.Max(1, options.LockoutMinutes)
        };
    }

    private sealed record LoginAttemptState(
        int FailureCount,
        DateTimeOffset WindowExpiresAtUtc,
        DateTimeOffset? LockedUntilUtc);
}

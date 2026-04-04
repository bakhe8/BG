using System.Net;
using BG.Application.Contracts.Services;
using BG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BG.Infrastructure.Identity;

internal sealed class DbLoginAttemptLockoutService : ILoginAttemptLockoutService
{
    private readonly BgDbContext _dbContext;
    private readonly IOptions<LoginLockoutOptions> _options;

    public DbLoginAttemptLockoutService(
        BgDbContext dbContext,
        IOptions<LoginLockoutOptions> options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public LoginLockoutDecision GetDecision(string? username, IPAddress? remoteIpAddress)
    {
        var key = BuildKey(username, remoteIpAddress);
        var now = DateTimeOffset.UtcNow;

        var record = _dbContext.LoginAttemptRecords
            .AsNoTracking()
            .FirstOrDefault(r => r.TrackingKey == key);

        if (record is null || record.WindowExpiresAtUtc <= now)
        {
            return new LoginLockoutDecision(false, 0, null);
        }

        if (record.LockedUntilUtc.HasValue && record.LockedUntilUtc.Value > now)
        {
            return new LoginLockoutDecision(true, record.FailureCount, record.LockedUntilUtc);
        }

        return new LoginLockoutDecision(false, record.FailureCount, null);
    }

    public LoginLockoutDecision RegisterFailure(string? username, IPAddress? remoteIpAddress)
    {
        var options = Normalize(_options.Value);
        var key = BuildKey(username, remoteIpAddress);
        var now = DateTimeOffset.UtcNow;

        var existing = _dbContext.LoginAttemptRecords
            .FirstOrDefault(r => r.TrackingKey == key);

        var failureCount = existing is not null && existing.WindowExpiresAtUtc > now
            ? existing.FailureCount + 1
            : 1;

        var windowExpiresAtUtc = now.AddMinutes(options.TrackingWindowMinutes);
        DateTimeOffset? lockedUntilUtc = null;

        if (failureCount >= options.MaxFailedAttempts)
        {
            lockedUntilUtc = now.AddMinutes(options.LockoutMinutes);
        }

        if (existing is null)
        {
            _dbContext.LoginAttemptRecords.Add(new LoginAttemptRecord
            {
                TrackingKey = key,
                FailureCount = failureCount,
                WindowExpiresAtUtc = windowExpiresAtUtc,
                LockedUntilUtc = lockedUntilUtc,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.FailureCount = failureCount;
            existing.WindowExpiresAtUtc = windowExpiresAtUtc;
            existing.LockedUntilUtc = lockedUntilUtc;
            existing.UpdatedAtUtc = now;
        }

        _dbContext.SaveChanges();

        return new LoginLockoutDecision(lockedUntilUtc.HasValue, failureCount, lockedUntilUtc);
    }

    public void Reset(string? username, IPAddress? remoteIpAddress)
    {
        var key = BuildKey(username, remoteIpAddress);
        var existing = _dbContext.LoginAttemptRecords
            .FirstOrDefault(r => r.TrackingKey == key);

        if (existing is not null)
        {
            _dbContext.LoginAttemptRecords.Remove(existing);
            _dbContext.SaveChanges();
        }
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
}

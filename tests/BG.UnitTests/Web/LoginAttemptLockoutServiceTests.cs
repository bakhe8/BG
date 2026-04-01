using System.Net;
using BG.Web.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BG.UnitTests.Web;

public sealed class LoginAttemptLockoutServiceTests
{
    [Fact]
    public void RegisterFailure_locks_identity_after_threshold()
    {
        var service = CreateService(maxFailedAttempts: 2);

        var first = service.RegisterFailure("request.user", IPAddress.Loopback);
        var second = service.RegisterFailure("request.user", IPAddress.Loopback);

        Assert.False(first.IsLockedOut);
        Assert.True(second.IsLockedOut);
        Assert.NotNull(second.LockedUntilUtc);
    }

    [Fact]
    public void Reset_clears_previous_failures()
    {
        var service = CreateService(maxFailedAttempts: 2);
        service.RegisterFailure("request.user", IPAddress.Loopback);

        service.Reset("request.user", IPAddress.Loopback);

        var decision = service.GetDecision("request.user", IPAddress.Loopback);
        Assert.False(decision.IsLockedOut);
        Assert.Equal(0, decision.FailureCount);
    }

    private static MemoryLoginAttemptLockoutService CreateService(int maxFailedAttempts)
    {
        return new MemoryLoginAttemptLockoutService(
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new LoginLockoutOptions
            {
                MaxFailedAttempts = maxFailedAttempts,
                TrackingWindowMinutes = 15,
                LockoutMinutes = 15
            }));
    }
}

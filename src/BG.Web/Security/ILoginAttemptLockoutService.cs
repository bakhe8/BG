using System.Net;

namespace BG.Web.Security;

public interface ILoginAttemptLockoutService
{
    LoginLockoutDecision GetDecision(string? username, IPAddress? remoteIpAddress);

    LoginLockoutDecision RegisterFailure(string? username, IPAddress? remoteIpAddress);

    void Reset(string? username, IPAddress? remoteIpAddress);
}

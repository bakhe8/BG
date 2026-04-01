using System.Security.Claims;
using BG.Application.Contracts.Services;

namespace BG.Web.Security;

public sealed class HttpContextExecutionActorAccessor : IExecutionActorAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextExecutionActorAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ExecutionActorSnapshot? GetCurrentActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : (Guid?)null;

        return new ExecutionActorSnapshot(
            userId,
            user.FindFirstValue("preferred_username"),
            user.FindFirstValue(ClaimTypes.Name));
    }
}

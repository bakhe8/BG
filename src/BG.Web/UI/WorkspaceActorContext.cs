using System.Security.Claims;

namespace BG.Web.UI;

public static class WorkspaceActorContext
{
    public static Guid? TryGetLockedActorUserId(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

using System.Security.Claims;
using BG.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;

namespace BG.Web.Security;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IUserAccessProfileService _userAccessProfileService;

    public PermissionAuthorizationHandler(IUserAccessProfileService userAccessProfileService)
    {
        _userAccessProfileService = userAccessProfileService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true || requirement.PermissionKeys.Count == 0)
        {
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var profile = await _userAccessProfileService.GetProfileAsync(userId);
        if (profile is null)
        {
            return;
        }

        if (requirement.PermissionKeys.Any(permissionKey =>
                profile.PermissionKeys.Contains(permissionKey, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }
    }
}

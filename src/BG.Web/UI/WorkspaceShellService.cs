using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using System.Security.Claims;

namespace BG.Web.UI;

public sealed class WorkspaceShellService : IWorkspaceShellService
{
    private static readonly WorkspaceModuleDefinition[] Modules =
    [
        new("NavHome", "/Index", []),
        new("NavIntake", "/Intake/Workspace", ["intake.view", "intake.scan", "intake.verify", "intake.finalize"]),
        new("NavOperationsQueue", "/Operations/Queue", ["operations.queue.view", "operations.queue.manage"]),
        new("NavRequests", "/Requests/Workspace", ["requests.view", "requests.create"]),
        new("NavApprovals", "/Approvals/Queue", ["approvals.queue.view", "approvals.sign"]),
        new("NavDispatch", "/Dispatch/Workspace", ["dispatch.view", "dispatch.print", "dispatch.record", "dispatch.email"]),
        new("NavUsers", "/Administration/Users", ["users.manage"]),
        new("NavDelegations", "/Administration/Delegations", ["delegations.manage"]),
        new("NavRoles", "/Administration/Roles", ["roles.manage"]),
        new("NavBanks", "/Administration/Banks", ["administration.banks"]),
        new("NavWorkflowAdmin", "/Administration/Workflow", ["workflow.manage"]),
        new("NavReports", "/Reports/Portfolio", ["reports.view"])
    ];

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAccessProfileService _userAccessProfileService;

    public WorkspaceShellService(
        IHttpContextAccessor httpContextAccessor,
        IUserAccessProfileService userAccessProfileService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userAccessProfileService = userAccessProfileService;
    }

    public Guid? GetAuthenticatedUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public async Task<WorkspaceShellSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var navigationItems = Modules
            .Where(module => module.PermissionKeys.Count == 0 ||
                             (currentUser is not null && module.PermissionKeys.Any(permissionKey =>
                                 currentUser.PermissionKeys.Contains(permissionKey, StringComparer.OrdinalIgnoreCase))))
            .Select(module => new WorkspaceShellNavigationItem(module.ResourceKey, module.PagePath))
            .ToArray();

        return new WorkspaceShellSnapshot(currentUser, Array.Empty<WorkspaceUserOptionDto>(), navigationItems);
    }

    public async Task<bool> CurrentUserHasAnyPermissionAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        var requiredPermissions = permissionKeys
            .Where(permissionKey => !string.IsNullOrWhiteSpace(permissionKey))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requiredPermissions.Length == 0)
        {
            return true;
        }

        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return false;
        }

        return requiredPermissions.Any(permissionKey =>
            currentUser.PermissionKeys.Contains(permissionKey, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> GetRequiredPermissionKeys(PathString path)
    {
        var matchedModule = Modules.FirstOrDefault(module =>
            module.PermissionKeys.Count > 0 &&
            path.StartsWithSegments(module.PagePath, StringComparison.OrdinalIgnoreCase));

        return matchedModule?.PermissionKeys ?? Array.Empty<string>();
    }

    private async Task<UserAccessProfileDto?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(typeof(UserAccessProfileDto), out var cachedProfile))
        {
            return cachedProfile as UserAccessProfileDto;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Items[typeof(UserAccessProfileDto)] = null!;
            return null;
        }

        var userId = GetAuthenticatedUserId();
        if (!userId.HasValue)
        {
            httpContext.Items[typeof(UserAccessProfileDto)] = null!;
            return null;
        }

        var profile = await _userAccessProfileService.GetProfileAsync(userId.Value, cancellationToken);
        httpContext.Items[typeof(UserAccessProfileDto)] = profile!;
        return profile;
    }

    private sealed record WorkspaceModuleDefinition(
        string ResourceKey,
        string PagePath,
        IReadOnlyList<string> PermissionKeys);
}

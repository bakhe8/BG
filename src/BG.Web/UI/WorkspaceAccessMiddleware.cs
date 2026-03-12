namespace BG.Web.UI;

public sealed class WorkspaceAccessMiddleware
{
    private readonly RequestDelegate _next;

    public WorkspaceAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IWorkspaceShellService workspaceShellService)
    {
        if (IsPublicPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var requiredPermissions = workspaceShellService.GetRequiredPermissionKeys(context.Request.Path);
        if (requiredPermissions.Count == 0)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true || !workspaceShellService.GetAuthenticatedUserId().HasValue)
        {
            context.Response.Redirect($"/SignIn?returnUrl={Uri.EscapeDataString(GetReturnUrl(context))}&shellMessage=WorkspaceShell_SignInRequired");
            return;
        }

        if (!await workspaceShellService.CurrentUserHasAnyPermissionAsync(requiredPermissions, context.RequestAborted))
        {
            context.Response.Redirect("/?shellMessage=WorkspaceShell_AccessDenied");
            return;
        }

        await _next(context);
    }

    private static bool IsPublicPath(PathString path)
    {
        return path == "/" ||
               path.StartsWithSegments("/Index", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/SignIn", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/Error", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/Privacy", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/system", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetReturnUrl(HttpContext context)
    {
        return string.Concat(context.Request.Path, context.Request.QueryString);
    }
}

using BG.Application.Contracts.Services;
using BG.Web.UI;
using BG.Application.Models.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IHomeDashboardService _homeDashboardService;
    private readonly IWorkspaceShellService _workspaceShellService;

    public IndexModel(
        IHomeDashboardService homeDashboardService,
        IWorkspaceShellService workspaceShellService)
    {
        _homeDashboardService = homeDashboardService;
        _workspaceShellService = workspaceShellService;
    }

    public HomeDashboardSnapshotDto Dashboard { get; private set; } = HomeDashboardSnapshotDto.Anonymous();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _homeDashboardService.GetSnapshotAsync(
            _workspaceShellService.GetAuthenticatedUserId(),
            cancellationToken);

        if (Dashboard.IsAuthenticated)
        {
            var shell = await _workspaceShellService.GetSnapshotAsync(cancellationToken);
            var operationalItems = shell.NavigationItems
                .Where(item =>
                    !string.Equals(item.PagePath, "/Index", StringComparison.OrdinalIgnoreCase) &&
                    !item.PagePath.StartsWith("/Administration", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var administrationItems = shell.NavigationItems
                .Where(item => item.PagePath.StartsWith("/Administration", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (operationalItems.Length == 1 && administrationItems.Length == 0)
            {
                return Redirect(operationalItems[0].PagePath);
            }
        }

        return Page();
    }
}

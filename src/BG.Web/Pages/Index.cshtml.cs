using BG.Application.Contracts.Services;
using BG.Web.UI;
using BG.Application.Models.Dashboard;
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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _homeDashboardService.GetSnapshotAsync(
            _workspaceShellService.GetAuthenticatedUserId(),
            cancellationToken);
    }
}

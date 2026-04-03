using BG.Application.Contracts.Services;
using BG.Application.Models.Dashboard;
using BG.Application.Models.Identity;
using BG.Web.Pages;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.UnitTests.Web;

public sealed class IndexPageTests
{
    [Fact]
    public async Task OnGetAsync_redirects_authenticated_single_workspace_user_to_workspace()
    {
        var userId = Guid.NewGuid();
        var dashboard = new HomeDashboardSnapshotDto(
            true,
            "Request User",
            false,
            true,
            false,
            false,
            false,
            false,
            0,
            [],
            0,
            [],
            0,
            0,
            0,
            0,
            [],
            []);

        var model = new IndexModel(
            new StubHomeDashboardService(dashboard),
            new StubWorkspaceShellService(
                userId,
                [
                    new WorkspaceShellNavigationItem("NavHome", "/Index"),
                    new WorkspaceShellNavigationItem("NavRequests", "/Requests/Workspace")
                ]));

        var result = await model.OnGetAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Requests/Workspace", redirect.Url);
    }

    [Fact]
    public async Task OnGetAsync_returns_page_for_multi_workspace_user()
    {
        var userId = Guid.NewGuid();
        var dashboard = new HomeDashboardSnapshotDto(
            true,
            "System Admin",
            true,
            true,
            false,
            false,
            false,
            true,
            1,
            [],
            0,
            [],
            0,
            0,
            0,
            0,
            [],
            []);

        var model = new IndexModel(
            new StubHomeDashboardService(dashboard),
            new StubWorkspaceShellService(
                userId,
                [
                    new WorkspaceShellNavigationItem("NavHome", "/Index"),
                    new WorkspaceShellNavigationItem("NavRequests", "/Requests/Workspace"),
                    new WorkspaceShellNavigationItem("NavApprovals", "/Approvals/Queue")
                ]));

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("System Admin", model.Dashboard.DisplayName);
    }

    private sealed class StubHomeDashboardService : IHomeDashboardService
    {
        private readonly HomeDashboardSnapshotDto _snapshot;

        public StubHomeDashboardService(HomeDashboardSnapshotDto snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<HomeDashboardSnapshotDto> GetSnapshotAsync(Guid? userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshot);
        }
    }

    private sealed class StubWorkspaceShellService : IWorkspaceShellService
    {
        private readonly Guid _userId;
        private readonly IReadOnlyList<WorkspaceShellNavigationItem> _navigationItems;

        public StubWorkspaceShellService(Guid userId, IReadOnlyList<WorkspaceShellNavigationItem> navigationItems)
        {
            _userId = userId;
            _navigationItems = navigationItems;
        }

        public Guid? GetAuthenticatedUserId() => _userId;

        public Task<WorkspaceShellSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkspaceShellSnapshot(
                    new UserAccessProfileDto(
                        _userId,
                        "request.user",
                        "Request User",
                        null,
                        ["Requester"],
                        ["requests.view", "requests.create"],
                        null,
                        null),
                    Array.Empty<WorkspaceUserOptionDto>(),
                    _navigationItems));
        }

        public Task<bool> CurrentUserHasAnyPermissionAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public IReadOnlyList<string> GetRequiredPermissionKeys(PathString path) => Array.Empty<string>();
    }
}

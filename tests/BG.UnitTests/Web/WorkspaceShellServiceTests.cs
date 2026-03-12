using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class WorkspaceShellServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_filters_navigation_to_authenticated_user_permissions()
    {
        var httpContext = new DefaultHttpContext();
        var userId = Guid.NewGuid();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ],
            WorkspaceShellDefaults.AuthenticationScheme));
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new WorkspaceShellService(accessor, new StubUserAccessProfileService(userId));

        var snapshot = await service.GetSnapshotAsync();

        Assert.NotNull(snapshot.CurrentUser);
        Assert.Contains(snapshot.NavigationItems, item => item.ResourceKey == "NavHome");
        Assert.Contains(snapshot.NavigationItems, item => item.ResourceKey == "NavRequests");
        Assert.DoesNotContain(snapshot.NavigationItems, item => item.ResourceKey == "NavApprovals");
        Assert.Empty(snapshot.AvailableUsers);
    }

    private sealed class StubUserAccessProfileService : IUserAccessProfileService
    {
        private readonly Guid _userId;

        public StubUserAccessProfileService(Guid userId)
        {
            _userId = userId;
        }

        public Task<IReadOnlyList<WorkspaceUserOptionDto>> ListActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkspaceUserOptionDto>>(
            [
                new WorkspaceUserOptionDto(_userId, "request.user", "Request User")
            ]);
        }

        public Task<UserAccessProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId != _userId)
            {
                return Task.FromResult<UserAccessProfileDto?>(null);
            }

            return Task.FromResult<UserAccessProfileDto?>(
                new UserAccessProfileDto(
                    _userId,
                    "request.user",
                    "Request User",
                    ["Requester"],
                    ["requests.view", "requests.create"]));
        }
    }
}

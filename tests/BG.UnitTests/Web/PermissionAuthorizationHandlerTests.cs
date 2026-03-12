using System.Security.Claims;
using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;

namespace BG.UnitTests.Web;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_succeeds_when_current_user_has_required_permission()
    {
        var userId = Guid.NewGuid();
        var handler = new PermissionAuthorizationHandler(new StubUserAccessProfileService(
            new UserAccessProfileDto(
                userId,
                "request.user",
                "Request User",
                ["Requester"],
                ["requests.view", "requests.create"])));
        var requirement = new PermissionAuthorizationRequirement(["requests.view"]);
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "test")),
            resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_does_not_succeed_when_profile_lacks_required_permission()
    {
        var userId = Guid.NewGuid();
        var handler = new PermissionAuthorizationHandler(new StubUserAccessProfileService(
            new UserAccessProfileDto(
                userId,
                "request.user",
                "Request User",
                ["Requester"],
                ["requests.view"])));
        var requirement = new PermissionAuthorizationRequirement(["approvals.sign"]);
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "test")),
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class StubUserAccessProfileService : IUserAccessProfileService
    {
        private readonly UserAccessProfileDto? _profile;

        public StubUserAccessProfileService(UserAccessProfileDto? profile)
        {
            _profile = profile;
        }

        public Task<IReadOnlyList<WorkspaceUserOptionDto>> ListActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkspaceUserOptionDto>>(Array.Empty<WorkspaceUserOptionDto>());
        }

        public Task<UserAccessProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profile?.Id == userId ? _profile : null);
        }
    }
}

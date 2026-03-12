using BG.Application.Contracts.Persistence;
using BG.Application.Services;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class UserAccessProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_returns_role_names_and_permission_keys_for_active_user()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var permission = new Permission("requests.view", "Requests");
        var role = new Role("Requester", "Creates requests");
        role.AssignPermissions([permission]);
        var user = new User("request.user", "Request User", "request.user@example.com", null, UserSourceType.Local, true, DateTimeOffset.UtcNow);
        user.AssignRoles([role]);
        repository.Users.Add(user);
        var service = new UserAccessProfileService(repository);

        var profile = await service.GetProfileAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Contains("Requester", profile.RoleNames);
        Assert.Contains("requests.view", profile.PermissionKeys);
    }

    private sealed class FakeIdentityAdministrationRepository : IIdentityAdministrationRepository
    {
        public List<User> Users { get; } = [];

        public Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users);
        }

        public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Role>>(Array.Empty<Role>());
        }

        public Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Permission>>(Array.Empty<Permission>());
        }

        public Task<IReadOnlyList<ApprovalDelegation>> ListApprovalDelegationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ApprovalDelegation>>(Array.Empty<ApprovalDelegation>());
        }

        public Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Role>>(Array.Empty<Role>());
        }

        public Task<IReadOnlyList<Permission>> GetPermissionsByKeysAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Permission>>(Array.Empty<Permission>());
        }

        public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<User?> GetUserByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.NormalizedUsername == normalizedUsername));
        }

        public Task<ApprovalDelegation?> GetApprovalDelegationByIdAsync(Guid delegationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ApprovalDelegation?>(null);
        }

        public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> RoleNameExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasOverlappingApprovalDelegationAsync(
            Guid delegateUserId,
            Guid roleId,
            DateTimeOffset startsAtUtc,
            DateTimeOffset endsAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AddApprovalDelegationAsync(ApprovalDelegation delegation, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

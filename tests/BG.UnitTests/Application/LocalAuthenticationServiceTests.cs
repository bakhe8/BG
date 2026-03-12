using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Identity;
using BG.Application.Models.Identity;
using BG.Application.Services;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class LocalAuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_returns_profile_for_valid_local_credentials()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var permission = new Permission("requests.view", "Requests");
        var role = new Role("Requester", "Creates requests");
        role.AssignPermissions([permission]);
        var user = new User("request.user", "Request User", "request.user@example.com", null, UserSourceType.Local, true, DateTimeOffset.UtcNow);
        user.SetLocalPassword("HASH:valid-password", DateTimeOffset.UtcNow);
        user.AssignRoles([role]);
        repository.Users.Add(user);
        var service = new LocalAuthenticationService(repository, new StubLocalPasswordHasher());

        var result = await service.AuthenticateAsync(new AuthenticateLocalUserCommand("request.user", "valid-password"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Contains("requests.view", result.Value.PermissionKeys);
    }

    [Fact]
    public async Task AuthenticateAsync_rejects_invalid_passwords()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var user = new User("request.user", "Request User", "request.user@example.com", null, UserSourceType.Local, true, DateTimeOffset.UtcNow);
        user.SetLocalPassword("HASH:valid-password", DateTimeOffset.UtcNow);
        repository.Users.Add(user);
        var service = new LocalAuthenticationService(repository, new StubLocalPasswordHasher());

        var result = await service.AuthenticateAsync(new AuthenticateLocalUserCommand("request.user", "wrong-password"));

        Assert.False(result.Succeeded);
        Assert.Equal(IdentityErrorCodes.InvalidCredentials, result.ErrorCode);
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

        public Task<bool> HasOverlappingApprovalDelegationAsync(Guid delegateUserId, Guid roleId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, CancellationToken cancellationToken = default)
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

    private sealed class StubLocalPasswordHasher : ILocalPasswordHasher
    {
        public string HashPassword(string password)
        {
            return $"HASH:{password}";
        }

        public bool VerifyPassword(string passwordHash, string password)
        {
            return passwordHash == HashPassword(password);
        }
    }
}

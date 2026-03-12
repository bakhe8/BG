using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Identity;
using BG.Application.Models.Identity;
using BG.Application.Services;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class IdentityAdministrationServiceTests
{
    [Fact]
    public async Task CreateRoleAsync_creates_role_with_selected_permissions()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var service = new IdentityAdministrationService(repository, new StubLocalPasswordHasher());

        var result = await service.CreateRoleAsync(
            new CreateRoleCommand(
                "Operations",
                "Operational role",
                ["users.view", "roles.view"]));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("Operations", result.Value.Name);
        Assert.Equal(2, result.Value.PermissionKeys.Count);
        Assert.Contains("users.view", result.Value.PermissionKeys);
        Assert.Single(repository.Roles);
    }

    [Fact]
    public async Task CreateUserAsync_returns_duplicate_error_for_existing_username()
    {
        var repository = new FakeIdentityAdministrationRepository();
        repository.Users.Add(new User(
            "admin",
            "Admin User",
            "admin@example.com",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow));

        var hasher = new StubLocalPasswordHasher();
        var service = new IdentityAdministrationService(repository, hasher);

        var result = await service.CreateUserAsync(
            new CreateUserCommand(
                "ADMIN",
                "Another Admin",
                "another@example.com",
                "admin-password",
                Array.Empty<Guid>()));

        Assert.False(result.Succeeded);
        Assert.Equal(IdentityErrorCodes.DuplicateUsername, result.ErrorCode);
    }

    [Fact]
    public async Task CreateUserAsync_assigns_roles_and_sets_local_source()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var role = new Role("Reviewer", "Guarantee reviewer");
        role.AssignPermissions(repository.Permissions.Take(1).ToArray());
        repository.Roles.Add(role);
        var hasher = new StubLocalPasswordHasher();

        var service = new IdentityAdministrationService(repository, hasher);

        var result = await service.CreateUserAsync(
            new CreateUserCommand(
                "reviewer.one",
                "Reviewer One",
                "reviewer.one@example.com",
                "reviewer-password",
                [role.Id]));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("Local", result.Value.SourceType);
        Assert.Contains("Reviewer", result.Value.Roles);
        Assert.True(result.Value.HasLocalPassword);
        Assert.Single(repository.Users);
        Assert.Equal("HASH:reviewer-password", repository.Users[0].PasswordHash);
    }

    [Fact]
    public async Task CreateUserAsync_rejects_short_passwords()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var service = new IdentityAdministrationService(repository, new StubLocalPasswordHasher());

        var result = await service.CreateUserAsync(
            new CreateUserCommand(
                "reviewer.one",
                "Reviewer One",
                "reviewer.one@example.com",
                "short",
                Array.Empty<Guid>()));

        Assert.False(result.Succeeded);
        Assert.Equal(IdentityErrorCodes.PasswordTooShort, result.ErrorCode);
    }

    [Fact]
    public async Task SetUserPasswordAsync_updates_existing_local_user_password()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var user = new User(
            "reviewer.one",
            "Reviewer One",
            "reviewer.one@example.com",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        repository.Users.Add(user);
        var service = new IdentityAdministrationService(repository, new StubLocalPasswordHasher());

        var result = await service.SetUserPasswordAsync(new SetLocalUserPasswordCommand(user.Id, "updated-password"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.HasLocalPassword);
        Assert.Equal("HASH:updated-password", user.PasswordHash);
    }

    private sealed class FakeIdentityAdministrationRepository : IIdentityAdministrationRepository
    {
        public List<User> Users { get; } = [];

        public List<Role> Roles { get; } = [];

        public List<Permission> Permissions { get; } =
        [
            new("dashboard.view", "Dashboard"),
            new("users.view", "Administration"),
            new("roles.view", "Administration"),
            new("guarantees.view", "Guarantees")
        ];

        public Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users);
        }

        public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Role>>(Roles);
        }

        public Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Permission>>(Permissions);
        }

        public Task<IReadOnlyList<ApprovalDelegation>> ListApprovalDelegationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ApprovalDelegation>>(Array.Empty<ApprovalDelegation>());
        }

        public Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
        {
            var ids = roleIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<Role>>(Roles.Where(role => ids.Contains(role.Id)).ToArray());
        }

        public Task<IReadOnlyList<Permission>> GetPermissionsByKeysAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default)
        {
            var keys = permissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyList<Permission>>(Permissions.Where(permission => keys.Contains(permission.Key)).ToArray());
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
            return Task.FromResult(Users.Any(user => user.NormalizedUsername == normalizedUsername));
        }

        public Task<bool> RoleNameExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Roles.Any(role => role.NormalizedName == normalizedRoleName));
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
            Roles.Add(role);
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

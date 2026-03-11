using BG.Application.Contracts.Persistence;
using BG.Application.Identity;
using BG.Application.Models.Identity;
using BG.Application.Services;
using BG.Domain.Identity;

namespace BG.UnitTests.Application;

public sealed class IdentityAdministrationServiceTests
{
    [Fact]
    public async Task CreateRoleAsync_creates_role_with_selected_permissions()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var service = new IdentityAdministrationService(repository);

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

        var service = new IdentityAdministrationService(repository);

        var result = await service.CreateUserAsync(
            new CreateUserCommand(
                "ADMIN",
                "Another Admin",
                "another@example.com",
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

        var service = new IdentityAdministrationService(repository);

        var result = await service.CreateUserAsync(
            new CreateUserCommand(
                "reviewer.one",
                "Reviewer One",
                "reviewer.one@example.com",
                [role.Id]));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("Local", result.Value.SourceType);
        Assert.Contains("Reviewer", result.Value.Roles);
        Assert.Single(repository.Users);
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

        public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.Any(user => user.NormalizedUsername == normalizedUsername));
        }

        public Task<bool> RoleNameExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Roles.Any(role => role.NormalizedName == normalizedRoleName));
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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

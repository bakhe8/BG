using BG.Application.Approvals;
using BG.Application.Contracts.Persistence;
using BG.Application.Models.Approvals;
using BG.Application.Services;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class ApprovalDelegationAdministrationServiceTests
{
    [Fact]
    public async Task CreateAsync_creates_delegation_when_delegator_owns_role()
    {
        var repository = new FakeApprovalDelegationAdministrationRepository();
        var role = repository.Roles[0];
        var delegator = repository.Users[0];
        var delegateUser = repository.Users[1];
        delegator.AssignRoles([role]);
        var service = new ApprovalDelegationAdministrationService(repository);

        var result = await service.CreateAsync(
            new CreateApprovalDelegationCommand(
                delegator.Id,
                delegateUser.Id,
                role.Id,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(9),
                "Vacation cover"));

        Assert.True(result.Succeeded);
        Assert.Single(repository.Delegations);
        Assert.Equal(delegateUser.Id, repository.Delegations[0].DelegateUserId);
    }

    [Fact]
    public async Task CreateAsync_rejects_overlapping_delegation_for_same_delegate_and_role()
    {
        var repository = new FakeApprovalDelegationAdministrationRepository();
        var role = repository.Roles[0];
        var delegator = repository.Users[0];
        var delegateUser = repository.Users[1];
        delegator.AssignRoles([role]);
        repository.Delegations.Add(new ApprovalDelegation(
            delegator.Id,
            delegateUser.Id,
            role.Id,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(9),
            "Existing",
            DateTimeOffset.UtcNow));
        var service = new ApprovalDelegationAdministrationService(repository);

        var result = await service.CreateAsync(
            new CreateApprovalDelegationCommand(
                delegator.Id,
                delegateUser.Id,
                role.Id,
                DateTimeOffset.UtcNow.AddHours(2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Overlap"));

        Assert.False(result.Succeeded);
        Assert.Equal(ApprovalDelegationErrorCodes.Overlap, result.ErrorCode);
    }

    [Fact]
    public async Task RevokeAsync_marks_delegation_as_revoked()
    {
        var repository = new FakeApprovalDelegationAdministrationRepository();
        var role = repository.Roles[0];
        var delegator = repository.Users[0];
        var delegateUser = repository.Users[1];
        delegator.AssignRoles([role]);
        var delegation = new ApprovalDelegation(
            delegator.Id,
            delegateUser.Id,
            role.Id,
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(8),
            "Coverage",
            DateTimeOffset.UtcNow.AddDays(-1));
        repository.Delegations.Add(delegation);
        var service = new ApprovalDelegationAdministrationService(repository);

        var result = await service.RevokeAsync(new RevokeApprovalDelegationCommand(delegation.Id, "Back from leave"));

        Assert.True(result.Succeeded);
        Assert.NotNull(delegation.RevokedAtUtc);
        Assert.Equal("Back from leave", delegation.RevocationReason);
    }

    private sealed class FakeApprovalDelegationAdministrationRepository : IIdentityAdministrationRepository
    {
        public FakeApprovalDelegationAdministrationRepository()
        {
            Users.Add(new User("delegator.one", "Delegator One", "delegator.one@example.com", null, UserSourceType.Local, true, DateTimeOffset.UtcNow));
            Users.Add(new User("delegate.one", "Delegate One", "delegate.one@example.com", null, UserSourceType.Local, true, DateTimeOffset.UtcNow));
            Permissions.Add(new Permission("approvals.queue.view", "Approvals"));
            var role = new Role("Guarantees Supervisor", "Approves guarantees");
            role.AssignPermissions(Permissions);
            Roles.Add(role);
        }

        public List<User> Users { get; } = [];

        public List<Role> Roles { get; } = [];

        public List<Permission> Permissions { get; } = [];

        public List<ApprovalDelegation> Delegations { get; } = [];

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
            return Task.FromResult<IReadOnlyList<ApprovalDelegation>>(Delegations);
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
            return Task.FromResult(Delegations.SingleOrDefault(delegation => delegation.Id == delegationId));
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
            return Task.FromResult(Delegations.Any(delegation =>
                delegation.DelegateUserId == delegateUserId &&
                delegation.RoleId == roleId &&
                delegation.Overlaps(startsAtUtc, endsAtUtc)));
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
            Delegations.Add(delegation);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

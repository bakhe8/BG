using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.Application.Contracts.Persistence;

public interface IIdentityAdministrationRepository
{
    Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalDelegation>> ListApprovalDelegationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> GetPermissionsByKeysAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetUserByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    Task<ApprovalDelegation?> GetApprovalDelegationByIdAsync(Guid delegationId, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    Task<bool> RoleNameExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingApprovalDelegationAsync(
        Guid delegateUserId,
        Guid roleId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken = default);

    Task AddUserAsync(User user, CancellationToken cancellationToken = default);

    Task AddRoleAsync(Role role, CancellationToken cancellationToken = default);

    Task AddApprovalDelegationAsync(ApprovalDelegation delegation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

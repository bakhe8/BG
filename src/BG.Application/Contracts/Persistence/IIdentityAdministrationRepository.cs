using BG.Domain.Identity;

namespace BG.Application.Contracts.Persistence;

public interface IIdentityAdministrationRepository
{
    Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> GetPermissionsByKeysAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    Task<bool> RoleNameExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default);

    Task AddUserAsync(User user, CancellationToken cancellationToken = default);

    Task AddRoleAsync(Role role, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

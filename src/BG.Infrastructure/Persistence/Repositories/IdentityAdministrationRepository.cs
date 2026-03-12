using BG.Application.Contracts.Persistence;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class IdentityAdministrationRepository : IIdentityAdministrationRepository
{
    private readonly BgDbContext _dbContext;

    public IdentityAdministrationRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .Include(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Permissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalDelegation>> ListApprovalDelegationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ApprovalDelegations
            .Include(delegation => delegation.DelegatorUser)
            .Include(delegation => delegation.DelegateUser)
            .Include(delegation => delegation.Role)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesByIdsAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var ids = roleIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return Array.Empty<Role>();
        }

        return await _dbContext.Roles
            .Include(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .Where(role => ids.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsByKeysAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        var keys = permissionKeys
            .Where(permissionKey => !string.IsNullOrWhiteSpace(permissionKey))
            .Select(permissionKey => permissionKey.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (keys.Length == 0)
        {
            return Array.Empty<Permission>();
        }

        return await _dbContext.Permissions
            .Where(permission => keys.Contains(permission.Key))
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> GetUserByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .SingleOrDefaultAsync(
                user => user.NormalizedUsername == normalizedUsername,
                cancellationToken);
    }

    public Task<ApprovalDelegation?> GetApprovalDelegationByIdAsync(Guid delegationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ApprovalDelegations
            .Include(delegation => delegation.DelegatorUser)
            .Include(delegation => delegation.DelegateUser)
            .Include(delegation => delegation.Role)
            .SingleOrDefaultAsync(delegation => delegation.Id == delegationId, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(
            user => user.NormalizedUsername == normalizedUsername,
            cancellationToken);
    }

    public Task<bool> RoleNameExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles.AnyAsync(
            role => role.NormalizedName == normalizedRoleName,
            cancellationToken);
    }

    public Task<bool> HasOverlappingApprovalDelegationAsync(
        Guid delegateUserId,
        Guid roleId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ApprovalDelegations.AnyAsync(
            delegation =>
                delegation.DelegateUserId == delegateUserId &&
                delegation.RoleId == roleId &&
                !delegation.RevokedAtUtc.HasValue &&
                startsAtUtc <= delegation.EndsAtUtc &&
                endsAtUtc >= delegation.StartsAtUtc,
            cancellationToken);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles.AddAsync(role, cancellationToken).AsTask();
    }

    public Task AddApprovalDelegationAsync(ApprovalDelegation delegation, CancellationToken cancellationToken = default)
    {
        return _dbContext.ApprovalDelegations.AddAsync(delegation, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

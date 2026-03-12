using BG.Application.Models.Identity;
using BG.Domain.Identity;

namespace BG.Application.Identity;

internal static class UserAccessProfileMapper
{
    public static UserAccessProfileDto Map(User user)
    {
        return new UserAccessProfileDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.UserRoles
                .Select(userRole => userRole.Role.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(roleName => roleName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            user.UserRoles
                .SelectMany(userRole => userRole.Role.RolePermissions)
                .Select(rolePermission => rolePermission.PermissionKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(permissionKey => permissionKey, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }
}

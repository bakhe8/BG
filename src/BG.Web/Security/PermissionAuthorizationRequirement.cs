using Microsoft.AspNetCore.Authorization;

namespace BG.Web.Security;

public sealed class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public PermissionAuthorizationRequirement(IEnumerable<string> permissionKeys)
    {
        PermissionKeys = permissionKeys
            .Where(permissionKey => !string.IsNullOrWhiteSpace(permissionKey))
            .Select(permissionKey => permissionKey.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> PermissionKeys { get; }
}

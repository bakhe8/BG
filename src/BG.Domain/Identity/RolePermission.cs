namespace BG.Domain.Identity;

public sealed class RolePermission
{
    public RolePermission(Guid roleId, string permissionKey)
    {
        RoleId = roleId;
        PermissionKey = permissionKey;
    }

    private RolePermission()
    {
        PermissionKey = string.Empty;
    }

    public Guid RoleId { get; private set; }

    public string PermissionKey { get; private set; }

    public Role Role { get; internal set; } = default!;

    public Permission Permission { get; internal set; } = default!;
}

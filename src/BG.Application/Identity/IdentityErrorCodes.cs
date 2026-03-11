namespace BG.Application.Identity;

public static class IdentityErrorCodes
{
    public const string UsernameRequired = "identity.username_required";
    public const string DisplayNameRequired = "identity.display_name_required";
    public const string RoleNameRequired = "identity.role_name_required";
    public const string DuplicateUsername = "identity.duplicate_username";
    public const string DuplicateRoleName = "identity.duplicate_role_name";
    public const string RoleNotFound = "identity.role_not_found";
    public const string PermissionNotFound = "identity.permission_not_found";
}

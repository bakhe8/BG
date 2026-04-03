namespace BG.Application.Models.Identity;

public sealed record UserAccessProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<string> PermissionKeys,
    string? PreferredCulture,
    string? PreferredTheme);

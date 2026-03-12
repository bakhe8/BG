namespace BG.Application.Models.Identity;

public sealed record UserAccessProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<string> PermissionKeys);

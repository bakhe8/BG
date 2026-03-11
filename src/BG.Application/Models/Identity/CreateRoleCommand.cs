namespace BG.Application.Models.Identity;

public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    IReadOnlyCollection<string> PermissionKeys);

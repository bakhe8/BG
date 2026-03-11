namespace BG.Application.Models.Identity;

public sealed record RoleSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionKeys);

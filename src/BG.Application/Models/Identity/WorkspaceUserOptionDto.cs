namespace BG.Application.Models.Identity;

public sealed record WorkspaceUserOptionDto(
    Guid Id,
    string Username,
    string DisplayName);

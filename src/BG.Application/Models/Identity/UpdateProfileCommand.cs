namespace BG.Application.Models.Identity;

public sealed record UpdateProfileCommand(
    string DisplayName,
    string? Email,
    string? PreferredCulture,
    string? PreferredTheme);

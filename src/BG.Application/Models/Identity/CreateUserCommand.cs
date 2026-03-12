namespace BG.Application.Models.Identity;

public sealed record CreateUserCommand(
    string Username,
    string DisplayName,
    string? Email,
    string InitialPassword,
    IReadOnlyCollection<Guid> RoleIds);

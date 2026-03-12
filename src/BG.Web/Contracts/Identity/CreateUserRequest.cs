namespace BG.Web.Contracts.Identity;

public sealed class CreateUserRequest
{
    public string Username { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string InitialPassword { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
}

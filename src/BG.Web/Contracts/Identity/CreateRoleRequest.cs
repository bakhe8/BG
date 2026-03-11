namespace BG.Web.Contracts.Identity;

public sealed class CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public IReadOnlyCollection<string> PermissionKeys { get; init; } = Array.Empty<string>();
}

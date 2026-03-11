namespace BG.Domain.Identity;

public sealed class UserRole
{
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    private UserRole()
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public User User { get; internal set; } = default!;

    public Role Role { get; internal set; } = default!;
}

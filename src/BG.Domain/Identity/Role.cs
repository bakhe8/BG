namespace BG.Domain.Identity;

public sealed class Role
{
    public Role(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        NormalizedName = NormalizeNameKey(name);
        Description = NormalizeOptional(description, 256);
    }

    private Role()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string NormalizedName { get; private set; }

    public string? Description { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    public void AssignPermissions(IEnumerable<Permission> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        RolePermissions.Clear();

        foreach (var permission in permissions
                     .Where(permission => permission is not null)
                     .DistinctBy(permission => permission.Key))
        {
            RolePermissions.Add(new RolePermission(Id, permission.Key)
            {
                Role = this,
                Permission = permission
            });
        }
    }

    public static string NormalizeNameKey(string name)
    {
        return NormalizeRequired(name, nameof(name), 64).ToUpperInvariant();
    }

    private static string NormalizeName(string name)
    {
        return NormalizeRequired(name, nameof(name), 64);
    }

    private static string NormalizeRequired(string value, string paramName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Maximum length is {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Maximum length is {maxLength} characters.");
        }

        return normalized;
    }
}

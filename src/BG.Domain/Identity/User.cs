namespace BG.Domain.Identity;

public sealed class User
{
    public User(
        string username,
        string displayName,
        string? email,
        string? externalId,
        UserSourceType sourceType,
        bool isActive,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        Username = NormalizeUsername(username);
        NormalizedUsername = NormalizeUsernameKey(username);
        DisplayName = NormalizeRequired(displayName, nameof(displayName), 128);
        Email = NormalizeOptional(email, 256)?.ToLowerInvariant();
        ExternalId = NormalizeOptional(externalId, 128);
        SourceType = sourceType;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    private User()
    {
        Username = string.Empty;
        NormalizedUsername = string.Empty;
        DisplayName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; }

    public string NormalizedUsername { get; private set; }

    public string DisplayName { get; private set; }

    public string? Email { get; private set; }

    public string? ExternalId { get; private set; }

    public UserSourceType SourceType { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastSyncedAtUtc { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public void AssignRoles(IEnumerable<Role> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        UserRoles.Clear();

        foreach (var role in roles
                     .Where(role => role is not null)
                     .DistinctBy(role => role.Id))
        {
            UserRoles.Add(new UserRole(Id, role.Id)
            {
                User = this,
                Role = role
            });
        }
    }

    public static string NormalizeUsernameKey(string username)
    {
        return NormalizeRequired(username, nameof(username), 64).ToUpperInvariant();
    }

    private static string NormalizeUsername(string username)
    {
        return NormalizeRequired(username, nameof(username), 64).ToLowerInvariant();
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

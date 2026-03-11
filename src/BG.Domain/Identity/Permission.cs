namespace BG.Domain.Identity;

public sealed class Permission
{
    public Permission(string key, string area)
    {
        Key = NormalizeKey(key);
        Area = NormalizeRequired(area, nameof(area), 64);
    }

    private Permission()
    {
        Key = string.Empty;
        Area = string.Empty;
    }

    public string Key { get; private set; }

    public string Area { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private static string NormalizeKey(string key)
    {
        return NormalizeRequired(key, nameof(key), 128).ToLowerInvariant();
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
}

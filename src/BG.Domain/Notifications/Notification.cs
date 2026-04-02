using BG.Domain.Identity;

namespace BG.Domain.Notifications;

public sealed class Notification
{
    public Notification(
        string message,
        string? link,
        string requiredPermission,
        DateTimeOffset createdAtUtc,
        Guid? targetUserId = null)
    {
        Id = Guid.NewGuid();
        Message = NormalizeRequired(message, nameof(message), 512);
        Link = NormalizeOptional(link, 2048);
        RequiredPermission = NormalizeRequired(requiredPermission, nameof(requiredPermission), 128);
        CreatedAtUtc = createdAtUtc;
        TargetUserId = targetUserId;
        IsRead = false;
    }

    private Notification()
    {
        Message = string.Empty;
        RequiredPermission = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Message { get; private set; }

    public string? Link { get; private set; }

    public string RequiredPermission { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? TargetUserId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public void MarkAsRead(DateTimeOffset readAtUtc)
    {
        IsRead = true;
        ReadAtUtc = readAtUtc;
    }

    private static string NormalizeRequired(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }

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

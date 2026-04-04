using System.Net.Mail;

namespace BG.Domain.Guarantees;

public sealed class Bank
{
    public Bank(
        string canonicalName,
        string shortCode,
        string? officialEmail,
        bool isEmailDispatchEnabled,
        IReadOnlyList<GuaranteeDispatchChannel> supportedDispatchChannels,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        CanonicalName = NormalizeRequired(canonicalName, nameof(canonicalName), 128);
        ShortCode = NormalizeShortCode(shortCode);
        OfficialEmail = NormalizeEmail(officialEmail, isEmailDispatchEnabled);
        IsEmailDispatchEnabled = isEmailDispatchEnabled;
        SupportedDispatchChannels = NormalizeSupportedDispatchChannels(supportedDispatchChannels);
        Notes = NormalizeOptional(notes, nameof(notes), 1024);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    private Bank()
    {
        CanonicalName = string.Empty;
        ShortCode = string.Empty;
        SupportedDispatchChannels = Array.Empty<GuaranteeDispatchChannel>();
    }

    public Guid Id { get; private set; }

    public string CanonicalName { get; private set; }

    public string ShortCode { get; private set; }

    public string? OfficialEmail { get; private set; }

    public bool IsEmailDispatchEnabled { get; private set; }

    public IReadOnlyList<GuaranteeDispatchChannel> SupportedDispatchChannels { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Update(
        string canonicalName,
        string shortCode,
        string? officialEmail,
        bool isEmailDispatchEnabled,
        IReadOnlyList<GuaranteeDispatchChannel> supportedDispatchChannels,
        string? notes)
    {
        CanonicalName = NormalizeRequired(canonicalName, nameof(canonicalName), 128);
        ShortCode = NormalizeShortCode(shortCode);
        OfficialEmail = NormalizeEmail(officialEmail, isEmailDispatchEnabled);
        IsEmailDispatchEnabled = isEmailDispatchEnabled;
        SupportedDispatchChannels = NormalizeSupportedDispatchChannels(supportedDispatchChannels);
        Notes = NormalizeOptional(notes, nameof(notes), 1024);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static string NormalizeShortCodeKey(string shortCode)
    {
        return NormalizeRequired(shortCode, nameof(shortCode), 16).ToUpperInvariant();
    }

    private static string NormalizeShortCode(string shortCode)
    {
        return NormalizeShortCodeKey(shortCode);
    }

    private static IReadOnlyList<GuaranteeDispatchChannel> NormalizeSupportedDispatchChannels(
        IReadOnlyList<GuaranteeDispatchChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);

        var normalized = channels
            .Distinct()
            .OrderBy(channel => channel)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one dispatch channel is required.", nameof(channels));
        }

        if (normalized.Any(channel => !Enum.IsDefined(channel)))
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Dispatch channel value is invalid.");
        }

        return normalized;
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

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Maximum length is {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? value, bool isEmailDispatchEnabled)
    {
        var normalized = NormalizeOptional(value, nameof(value), 256);

        if (isEmailDispatchEnabled && string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Official email is required when email dispatch is enabled.", nameof(value));
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        _ = new MailAddress(normalized);
        return normalized.ToLowerInvariant();
    }
}

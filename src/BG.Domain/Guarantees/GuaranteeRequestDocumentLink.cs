using BG.Domain.Identity;

namespace BG.Domain.Guarantees;

public sealed class GuaranteeRequestDocumentLink
{
    public GuaranteeRequestDocumentLink(
        Guid guaranteeRequestId,
        Guid guaranteeDocumentId,
        DateTimeOffset linkedAtUtc,
        Guid? linkedByUserId = null,
        string? linkedByDisplayName = null)
    {
        Id = Guid.NewGuid();
        GuaranteeRequestId = guaranteeRequestId;
        GuaranteeDocumentId = guaranteeDocumentId;
        LinkedAtUtc = linkedAtUtc;
        LinkedByUserId = linkedByUserId;
        LinkedByDisplayName = NormalizeOptional(linkedByDisplayName, 256);
    }

    private GuaranteeRequestDocumentLink()
    {
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeRequestId { get; private set; }

    public Guid GuaranteeDocumentId { get; private set; }

    public DateTimeOffset LinkedAtUtc { get; private set; }

    public Guid? LinkedByUserId { get; private set; }

    public string? LinkedByDisplayName { get; private set; }

    public GuaranteeRequest GuaranteeRequest { get; internal set; } = default!;

    public GuaranteeDocument GuaranteeDocument { get; internal set; } = default!;

    public User? LinkedByUser { get; internal set; }

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

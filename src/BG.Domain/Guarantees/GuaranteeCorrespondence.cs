namespace BG.Domain.Guarantees;

public sealed class GuaranteeCorrespondence
{
    public GuaranteeCorrespondence(
        Guid guaranteeId,
        Guid? guaranteeRequestId,
        GuaranteeCorrespondenceDirection direction,
        GuaranteeCorrespondenceKind kind,
        string referenceNumber,
        DateOnly letterDate,
        Guid? scannedDocumentId,
        string? notes,
        DateTimeOffset registeredAtUtc)
    {
        Id = Guid.NewGuid();
        GuaranteeId = guaranteeId;
        GuaranteeRequestId = guaranteeRequestId;
        Direction = direction;
        Kind = kind;
        ReferenceNumber = NormalizeRequired(referenceNumber, nameof(referenceNumber), 64);
        LetterDate = letterDate;
        ScannedDocumentId = scannedDocumentId;
        Notes = NormalizeOptional(notes, 1000);
        RegisteredAtUtc = registeredAtUtc;
    }

    private GuaranteeCorrespondence()
    {
        ReferenceNumber = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public Guid? GuaranteeRequestId { get; private set; }

    public GuaranteeCorrespondenceDirection Direction { get; private set; }

    public GuaranteeCorrespondenceKind Kind { get; private set; }

    public string ReferenceNumber { get; private set; }

    public DateOnly LetterDate { get; private set; }

    public Guid? ScannedDocumentId { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset RegisteredAtUtc { get; private set; }

    public DateTimeOffset? AppliedToGuaranteeAtUtc { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public GuaranteeRequest? GuaranteeRequest { get; internal set; }

    public GuaranteeDocument? ScannedDocument { get; internal set; }

    internal void MarkAppliedToGuarantee(DateTimeOffset appliedAtUtc)
    {
        AppliedToGuaranteeAtUtc = appliedAtUtc;
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

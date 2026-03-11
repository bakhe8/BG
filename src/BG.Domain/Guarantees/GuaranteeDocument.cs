namespace BG.Domain.Guarantees;

public sealed class GuaranteeDocument
{
    public GuaranteeDocument(
        Guid guaranteeId,
        GuaranteeDocumentType documentType,
        GuaranteeDocumentSourceType sourceType,
        string fileName,
        string storagePath,
        int pageCount,
        DateTimeOffset capturedAtUtc,
        string? notes = null)
    {
        if (pageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), "Page count must be greater than zero.");
        }

        Id = Guid.NewGuid();
        GuaranteeId = guaranteeId;
        DocumentType = documentType;
        SourceType = sourceType;
        FileName = NormalizeRequired(fileName, nameof(fileName), 260);
        StoragePath = NormalizeRequired(storagePath, nameof(storagePath), 512);
        PageCount = pageCount;
        CapturedAtUtc = capturedAtUtc;
        Notes = NormalizeOptional(notes, 1000);
    }

    private GuaranteeDocument()
    {
        FileName = string.Empty;
        StoragePath = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public GuaranteeDocumentType DocumentType { get; private set; }

    public GuaranteeDocumentSourceType SourceType { get; private set; }

    public string FileName { get; private set; }

    public string StoragePath { get; private set; }

    public int PageCount { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public ICollection<GuaranteeCorrespondence> Correspondence { get; private set; } = new List<GuaranteeCorrespondence>();

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

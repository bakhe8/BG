using BG.Domain.Identity;

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
        Guid? capturedByUserId = null,
        string? capturedByDisplayName = null,
        GuaranteeDocumentCaptureChannel captureChannel = GuaranteeDocumentCaptureChannel.ManualUpload,
        string? sourceSystemName = null,
        string? sourceReference = null,
        string? intakeScenarioKey = null,
        string? extractionMethod = null,
        string? verifiedDataJson = null,
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
        CapturedByUserId = capturedByUserId;
        CapturedByDisplayName = NormalizeOptional(capturedByDisplayName, nameof(capturedByDisplayName), 256);
        CaptureChannel = captureChannel;
        SourceSystemName = NormalizeOptional(sourceSystemName, nameof(sourceSystemName), 128);
        SourceReference = NormalizeOptional(sourceReference, nameof(sourceReference), 128);
        IntakeScenarioKey = NormalizeOptional(intakeScenarioKey, nameof(intakeScenarioKey), 64);
        ExtractionMethod = NormalizeOptional(extractionMethod, nameof(extractionMethod), 64);
        VerifiedDataJson = NormalizeOptional(verifiedDataJson, nameof(verifiedDataJson), 12000);
        Notes = NormalizeOptional(notes, nameof(notes), 1000);
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

    public Guid? CapturedByUserId { get; private set; }

    public string? CapturedByDisplayName { get; private set; }

    public GuaranteeDocumentCaptureChannel CaptureChannel { get; private set; }

    public string? SourceSystemName { get; private set; }

    public string? SourceReference { get; private set; }

    public string? IntakeScenarioKey { get; private set; }

    public string? ExtractionMethod { get; private set; }

    public string? VerifiedDataJson { get; private set; }

    public string? Notes { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public User? CapturedByUser { get; internal set; }

    public ICollection<GuaranteeCorrespondence> Correspondence { get; private set; } = new List<GuaranteeCorrespondence>();

    public ICollection<GuaranteeRequestDocumentLink> RequestLinks { get; private set; } = new List<GuaranteeRequestDocumentLink>();

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
}

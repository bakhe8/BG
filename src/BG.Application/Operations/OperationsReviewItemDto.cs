using BG.Application.Models.Documents;

namespace BG.Application.Operations;

public sealed class OperationsReviewItemDto
{
    public OperationsReviewItemDto(
        Guid id,
        string guaranteeNumber,
        string scenarioKey,
        string scenarioTitleResourceKey,
        string categoryResourceKey,
        string statusResourceKey,
        string recommendedLaneResourceKey,
        string fileName,
        string? bankReference,
        DateTimeOffset createdAtUtc,
        DateTimeOffset capturedAtUtc,
        string? capturedByDisplayName,
        string captureChannelResourceKey,
        string? sourceSystemName,
        string? sourceReference,
        GuaranteeDocumentFormSnapshotDto? documentForm,
        bool supportsRequestMatching,
        IReadOnlyList<OperationsUnifiedSuggestion> matchSuggestions,
        string? suggestedConfirmedExpiryDate,
        string? suggestedConfirmedValue,
        string? suggestedStatusStatement,
        bool requiresConfirmedExpiryDate,
        bool requiresConfirmedAmount,
        DateTimeOffset? completedAtUtc)
    {
        Id = id;
        GuaranteeNumber = guaranteeNumber;
        ScenarioKey = scenarioKey;
        ScenarioTitleResourceKey = scenarioTitleResourceKey;
        CategoryResourceKey = categoryResourceKey;
        StatusResourceKey = statusResourceKey;
        RecommendedLaneResourceKey = recommendedLaneResourceKey;
        FileName = fileName;
        BankReference = bankReference;
        CreatedAtUtc = createdAtUtc;
        CapturedAtUtc = capturedAtUtc;
        CapturedByDisplayName = capturedByDisplayName;
        CaptureChannelResourceKey = captureChannelResourceKey;
        SourceSystemName = sourceSystemName;
        SourceReference = sourceReference;
        DocumentForm = documentForm;
        SupportsRequestMatching = supportsRequestMatching;
        MatchSuggestions = matchSuggestions;
        SuggestedConfirmedExpiryDate = suggestedConfirmedExpiryDate;
        SuggestedConfirmedValue = suggestedConfirmedValue;
        SuggestedStatusStatement = suggestedStatusStatement;
        RequiresConfirmedExpiryDate = requiresConfirmedExpiryDate;
        RequiresConfirmedAmount = requiresConfirmedAmount;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; }
    public string GuaranteeNumber { get; }
    public string ScenarioKey { get; }
    public string ScenarioTitleResourceKey { get; }
    public string CategoryResourceKey { get; }
    public string StatusResourceKey { get; }
    public string RecommendedLaneResourceKey { get; }
    public string FileName { get; }
    public string? BankReference { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset CapturedAtUtc { get; }
    public string? CapturedByDisplayName { get; }
    public string CaptureChannelResourceKey { get; }
    public string? SourceSystemName { get; }
    public string? SourceReference { get; }
    public GuaranteeDocumentFormSnapshotDto? DocumentForm { get; }
    public bool SupportRequestMatching => SupportsRequestMatching;
    public bool SupportsRequestMatching { get; }
    public IReadOnlyList<OperationsUnifiedSuggestion> MatchSuggestions { get; }
    public string? SuggestedConfirmedExpiryDate { get; }
    public string? SuggestedConfirmedValue { get; }
    public string? SuggestedStatusStatement { get; }
    public bool RequiresConfirmedExpiryDate { get; }
    public bool RequiresConfirmedAmount { get; }
    public DateTimeOffset? CompletedAtUtc { get; }
}

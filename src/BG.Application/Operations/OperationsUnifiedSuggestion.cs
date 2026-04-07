using BG.Application.Models.Documents;

namespace BG.Application.Operations;

public sealed class OperationsUnifiedSuggestion
{
    public OperationsUnifiedSuggestion(
        Guid requestId,
        string requestTypeResourceKey,
        string statusResourceKey,
        int score,
        string confidenceResourceKey,
        DateTimeOffset? submittedToBankAtUtc,
        string? outgoingReferenceNumber,
        IReadOnlyList<string> reasonResourceKeys,
        GuaranteeDocumentFormSnapshotDto? requestDocumentForm,
        bool isSelectionBlocked,
        string? blockingReasonResourceKey)
    {
        RequestId = requestId;
        RequestTypeResourceKey = requestTypeResourceKey;
        StatusResourceKey = statusResourceKey;
        Score = score;
        ConfidenceResourceKey = confidenceResourceKey;
        SubmittedToBankAtUtc = submittedToBankAtUtc;
        OutgoingReferenceNumber = outgoingReferenceNumber;
        ReasonResourceKeys = reasonResourceKeys;
        RequestDocumentForm = requestDocumentForm;
        IsSelectionBlocked = isSelectionBlocked;
        BlockingReasonResourceKey = blockingReasonResourceKey;
    }

    public Guid RequestId { get; }
    public string RequestTypeResourceKey { get; }
    public string StatusResourceKey { get; }
    public int Score { get; }
    public string ConfidenceResourceKey { get; }
    public DateTimeOffset? SubmittedToBankAtUtc { get; }
    public string? OutgoingReferenceNumber { get; }
    public IReadOnlyList<string> ReasonResourceKeys { get; }
    public GuaranteeDocumentFormSnapshotDto? RequestDocumentForm { get; }
    public bool IsSelectionBlocked { get; }
    public string? BlockingReasonResourceKey { get; }
}

public sealed class OperationsReviewRecentItemDto
{
    public OperationsReviewRecentItemDto(
        Guid id,
        string guaranteeNumber,
        string statusResourceKey,
        DateTimeOffset completedAtUtc,
        string? bankReference,
        string scenarioTitleResourceKey)
    {
        Id = id;
        GuaranteeNumber = guaranteeNumber;
        StatusResourceKey = statusResourceKey;
        CompletedAtUtc = completedAtUtc;
        BankReference = bankReference;
        ScenarioTitleResourceKey = scenarioTitleResourceKey;
    }

    public Guid Id { get; }
    public string GuaranteeNumber { get; }
    public string StatusResourceKey { get; }
    public DateTimeOffset CompletedAtUtc { get; }
    public string? BankReference { get; }
    public string ScenarioTitleResourceKey { get; }
}

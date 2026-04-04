using BG.Domain.Guarantees;

namespace BG.Domain.Operations;

public sealed class OperationsReviewItem
{
    public OperationsReviewItem(
        Guid guaranteeId,
        string guaranteeNumber,
        Guid guaranteeDocumentId,
        Guid? guaranteeCorrespondenceId,
        string scenarioKey,
        OperationsReviewItemCategory category,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        GuaranteeId = guaranteeId;
        GuaranteeNumber = NormalizeRequired(guaranteeNumber, nameof(guaranteeNumber), 64);
        GuaranteeDocumentId = guaranteeDocumentId;
        GuaranteeCorrespondenceId = guaranteeCorrespondenceId;
        ScenarioKey = NormalizeRequired(scenarioKey, nameof(scenarioKey), 64);
        Category = category;
        Status = OperationsReviewItemStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    private OperationsReviewItem()
    {
        GuaranteeNumber = string.Empty;
        ScenarioKey = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public string GuaranteeNumber { get; private set; }

    public Guid GuaranteeDocumentId { get; private set; }

    public Guid? GuaranteeCorrespondenceId { get; private set; }

    public string ScenarioKey { get; private set; }

    public OperationsReviewItemCategory Category { get; private set; }

    public OperationsReviewItemStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? RoutedAtUtc { get; private set; }

    public string? RoutedToLaneKey { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guarantee Guarantee { get; private set; } = default!;

    public GuaranteeDocument GuaranteeDocument { get; private set; } = default!;

    public GuaranteeCorrespondence? GuaranteeCorrespondence { get; private set; }

    public void RouteTo(string laneKey, DateTimeOffset routedAtUtc)
    {
        if (Status == OperationsReviewItemStatus.Completed)
        {
            throw new InvalidOperationException("A completed review item cannot be routed again.");
        }

        RoutedToLaneKey = NormalizeRequired(laneKey, nameof(laneKey), 64);
        RoutedAtUtc = routedAtUtc;
        Status = OperationsReviewItemStatus.Routed;
    }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        if (Status == OperationsReviewItemStatus.Completed)
        {
            throw new InvalidOperationException("The review item is already completed.");
        }

        CompletedAtUtc = completedAtUtc;
        Status = OperationsReviewItemStatus.Completed;
    }

    public void ReopenForCorrection(DateTimeOffset reopenedAtUtc)
    {
        if (Status != OperationsReviewItemStatus.Completed)
        {
            throw new InvalidOperationException("Only completed review items can be reopened for correction.");
        }

        CompletedAtUtc = null;

        if (!string.IsNullOrWhiteSpace(RoutedToLaneKey))
        {
            RoutedAtUtc = reopenedAtUtc;
            Status = OperationsReviewItemStatus.Routed;
            return;
        }

        Status = OperationsReviewItemStatus.Pending;
    }

    public void MarkReturned(DateTimeOffset returnedAtUtc)
    {
        if (Status is OperationsReviewItemStatus.Returned or OperationsReviewItemStatus.Rejected)
        {
            throw new InvalidOperationException("The review item has already been dismissed.");
        }

        CompletedAtUtc = returnedAtUtc;
        Status = OperationsReviewItemStatus.Returned;
    }

    public void MarkRejected(DateTimeOffset rejectedAtUtc)
    {
        if (Status is OperationsReviewItemStatus.Returned or OperationsReviewItemStatus.Rejected)
        {
            throw new InvalidOperationException("The review item has already been dismissed.");
        }

        CompletedAtUtc = rejectedAtUtc;
        Status = OperationsReviewItemStatus.Rejected;
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

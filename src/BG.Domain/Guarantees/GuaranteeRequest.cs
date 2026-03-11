namespace BG.Domain.Guarantees;

public sealed class GuaranteeRequest
{
    public GuaranteeRequest(
        Guid guaranteeId,
        GuaranteeRequestType requestType,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        GuaranteeId = guaranteeId;
        RequestType = requestType;
        RequestedAmount = requestedAmount;
        RequestedExpiryDate = requestedExpiryDate;
        Notes = NormalizeOptional(notes, 1000);
        CreatedAtUtc = createdAtUtc;
        Status = GuaranteeRequestStatus.Draft;
    }

    private GuaranteeRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public GuaranteeRequestType RequestType { get; private set; }

    public GuaranteeRequestStatus Status { get; private set; }

    public decimal? RequestedAmount { get; private set; }

    public DateOnly? RequestedExpiryDate { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? SubmittedToBankAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid? CompletionCorrespondenceId { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public ICollection<GuaranteeCorrespondence> Correspondence { get; private set; } = new List<GuaranteeCorrespondence>();

    internal void MarkSubmittedToBank(DateTimeOffset submittedAtUtc)
    {
        if (Status == GuaranteeRequestStatus.Completed)
        {
            throw new InvalidOperationException("A completed request cannot be re-submitted.");
        }

        SubmittedToBankAtUtc ??= submittedAtUtc;
        Status = GuaranteeRequestStatus.AwaitingBankResponse;
    }

    internal void MarkCompleted(Guid correspondenceId, DateTimeOffset completedAtUtc)
    {
        if (Status == GuaranteeRequestStatus.Completed)
        {
            throw new InvalidOperationException("The request is already completed.");
        }

        CompletionCorrespondenceId = correspondenceId;
        CompletedAtUtc = completedAtUtc;
        Status = GuaranteeRequestStatus.Completed;
    }

    internal void AttachCorrespondence(GuaranteeCorrespondence correspondence)
    {
        if (correspondence.GuaranteeRequestId != Id)
        {
            throw new InvalidOperationException("Correspondence does not belong to this request.");
        }

        Correspondence.Add(correspondence);
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

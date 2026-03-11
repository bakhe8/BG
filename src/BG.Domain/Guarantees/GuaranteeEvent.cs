namespace BG.Domain.Guarantees;

public sealed class GuaranteeEvent
{
    private GuaranteeEvent(
        Guid guaranteeId,
        Guid? guaranteeRequestId,
        Guid? guaranteeCorrespondenceId,
        GuaranteeEventType eventType,
        DateTimeOffset occurredAtUtc,
        string summary,
        decimal? previousAmount,
        decimal? newAmount,
        DateOnly? previousExpiryDate,
        DateOnly? newExpiryDate,
        GuaranteeStatus? previousStatus,
        GuaranteeStatus? newStatus)
    {
        Id = Guid.NewGuid();
        GuaranteeId = guaranteeId;
        GuaranteeRequestId = guaranteeRequestId;
        GuaranteeCorrespondenceId = guaranteeCorrespondenceId;
        EventType = eventType;
        OccurredAtUtc = occurredAtUtc;
        Summary = NormalizeRequired(summary, nameof(summary), 512);
        PreviousAmount = previousAmount;
        NewAmount = newAmount;
        PreviousExpiryDate = previousExpiryDate;
        NewExpiryDate = newExpiryDate;
        PreviousStatus = previousStatus?.ToString();
        NewStatus = newStatus?.ToString();
    }

    private GuaranteeEvent()
    {
        Summary = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public Guid? GuaranteeRequestId { get; private set; }

    public Guid? GuaranteeCorrespondenceId { get; private set; }

    public GuaranteeEventType EventType { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Summary { get; private set; }

    public decimal? PreviousAmount { get; private set; }

    public decimal? NewAmount { get; private set; }

    public DateOnly? PreviousExpiryDate { get; private set; }

    public DateOnly? NewExpiryDate { get; private set; }

    public string? PreviousStatus { get; private set; }

    public string? NewStatus { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public GuaranteeRequest? GuaranteeRequest { get; internal set; }

    public GuaranteeCorrespondence? GuaranteeCorrespondence { get; internal set; }

    internal static GuaranteeEvent Registered(Guid guaranteeId, DateTimeOffset occurredAtUtc)
    {
        return new GuaranteeEvent(
            guaranteeId,
            null,
            null,
            GuaranteeEventType.Registered,
            occurredAtUtc,
            "Guarantee registered in BG.",
            null,
            null,
            null,
            null,
            null,
            GuaranteeStatus.Active);
    }

    internal static GuaranteeEvent RequestRecorded(
        Guid guaranteeId,
        Guid requestId,
        GuaranteeRequestType requestType,
        DateTimeOffset occurredAtUtc)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            GuaranteeEventType.RequestRecorded,
            occurredAtUtc,
            $"Request recorded: {requestType}.",
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent FromConfirmedChange(
        Guid guaranteeId,
        Guid requestId,
        Guid correspondenceId,
        GuaranteeEventType eventType,
        DateTimeOffset occurredAtUtc,
        string summary,
        decimal? previousAmount,
        decimal? newAmount,
        DateOnly? previousExpiryDate,
        DateOnly? newExpiryDate,
        GuaranteeStatus? previousStatus,
        GuaranteeStatus? newStatus)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            eventType,
            occurredAtUtc,
            summary,
            previousAmount,
            newAmount,
            previousExpiryDate,
            newExpiryDate,
            previousStatus,
            newStatus);
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

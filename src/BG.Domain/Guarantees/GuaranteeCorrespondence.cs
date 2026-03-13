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

    public int PrintCount { get; private set; }

    public GuaranteeOutgoingLetterPrintMode? LastPrintMode { get; private set; }

    public DateTimeOffset? LastPrintedAtUtc { get; private set; }

    public GuaranteeDispatchChannel? DispatchChannel { get; private set; }

    public string? DispatchReference { get; private set; }

    public string? DispatchNote { get; private set; }

    public DateTimeOffset? DispatchedAtUtc { get; private set; }

    public string? DeliveryReference { get; private set; }

    public string? DeliveryNote { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public DateTimeOffset RegisteredAtUtc { get; private set; }

    public DateTimeOffset? AppliedToGuaranteeAtUtc { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public GuaranteeRequest? GuaranteeRequest { get; internal set; }

    public GuaranteeDocument? ScannedDocument { get; internal set; }

    internal void EnsureMatchesOutgoingReference(string referenceNumber, DateOnly letterDate)
    {
        EnsureOutgoingRequestLetter();

        if (!string.Equals(ReferenceNumber, NormalizeRequired(referenceNumber, nameof(referenceNumber), 64), StringComparison.Ordinal) ||
            LetterDate != letterDate)
        {
            throw new InvalidOperationException("The outgoing letter reference or date does not match the existing correspondence record.");
        }
    }

    internal void RecordPrint(GuaranteeOutgoingLetterPrintMode printMode, DateTimeOffset printedAtUtc)
    {
        EnsureOutgoingRequestLetter();

        if (!Enum.IsDefined(printMode))
        {
            throw new ArgumentOutOfRangeException(nameof(printMode), "Print mode must be valid.");
        }

        if (DeliveredAtUtc.HasValue)
        {
            throw new InvalidOperationException("A delivered outgoing letter cannot be printed again.");
        }

        PrintCount += 1;
        LastPrintMode = printMode;
        LastPrintedAtUtc = printedAtUtc;
    }

    internal void RecordDispatch(
        GuaranteeDispatchChannel dispatchChannel,
        string? dispatchReference,
        string? dispatchNote,
        DateTimeOffset dispatchedAtUtc)
    {
        EnsureOutgoingRequestLetter();

        if (!Enum.IsDefined(dispatchChannel))
        {
            throw new ArgumentOutOfRangeException(nameof(dispatchChannel), "Dispatch channel must be valid.");
        }

        if (DispatchedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outgoing letter has already been dispatched.");
        }

        DispatchChannel = dispatchChannel;
        DispatchReference = NormalizeOptional(dispatchReference, 128);
        DispatchNote = NormalizeOptional(dispatchNote, 1000);
        DispatchedAtUtc = dispatchedAtUtc;
    }

    internal void ConfirmDelivery(
        string? deliveryReference,
        string? deliveryNote,
        DateTimeOffset deliveredAtUtc)
    {
        EnsureOutgoingRequestLetter();

        if (!DispatchedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outgoing letter must be dispatched before delivery confirmation.");
        }

        if (DeliveredAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outgoing letter delivery is already confirmed.");
        }

        if (deliveredAtUtc < DispatchedAtUtc.Value)
        {
            throw new InvalidOperationException("Delivery confirmation cannot be earlier than dispatch.");
        }

        DeliveryReference = NormalizeOptional(deliveryReference, 128);
        DeliveryNote = NormalizeOptional(deliveryNote, 1000);
        DeliveredAtUtc = deliveredAtUtc;
    }

    internal void ReopenDispatch()
    {
        EnsureOutgoingRequestLetter();

        if (!DispatchedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outgoing letter has not been dispatched.");
        }

        if (DeliveredAtUtc.HasValue)
        {
            throw new InvalidOperationException("A delivered outgoing letter cannot be reopened.");
        }

        if (AppliedToGuaranteeAtUtc.HasValue)
        {
            throw new InvalidOperationException("A bank-applied outgoing letter cannot be reopened.");
        }

        DispatchChannel = null;
        DispatchReference = null;
        DispatchNote = null;
        DispatchedAtUtc = null;
    }

    internal void MarkAppliedToGuarantee(DateTimeOffset appliedAtUtc)
    {
        AppliedToGuaranteeAtUtc = appliedAtUtc;
    }

    internal void LinkToRequest(GuaranteeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.GuaranteeId != GuaranteeId)
        {
            throw new InvalidOperationException("The request does not belong to the guarantee.");
        }

        if (GuaranteeRequestId.HasValue && GuaranteeRequestId.Value != request.Id)
        {
            throw new InvalidOperationException("The correspondence is already linked to another request.");
        }

        GuaranteeRequestId = request.Id;
        GuaranteeRequest = request;
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

    private void EnsureOutgoingRequestLetter()
    {
        if (Direction != GuaranteeCorrespondenceDirection.Outgoing || Kind != GuaranteeCorrespondenceKind.RequestLetter)
        {
            throw new InvalidOperationException("Dispatch lifecycle can only be recorded for outgoing request letters.");
        }
    }
}

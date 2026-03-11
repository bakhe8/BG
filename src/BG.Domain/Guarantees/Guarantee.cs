namespace BG.Domain.Guarantees;

public sealed class Guarantee
{
    public Guarantee(
        string guaranteeNumber,
        string bankName,
        string beneficiaryName,
        string principalName,
        decimal currentAmount,
        string currencyCode,
        DateOnly issueDate,
        DateOnly expiryDate,
        DateTimeOffset createdAtUtc,
        string? externalReference = null)
    {
        if (currentAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentAmount), "Guarantee amount must be greater than zero.");
        }

        if (expiryDate < issueDate)
        {
            throw new ArgumentOutOfRangeException(nameof(expiryDate), "Expiry date cannot be before issue date.");
        }

        Id = Guid.NewGuid();
        GuaranteeNumber = NormalizeRequired(guaranteeNumber, nameof(guaranteeNumber), 64);
        BankName = NormalizeRequired(bankName, nameof(bankName), 128);
        BeneficiaryName = NormalizeRequired(beneficiaryName, nameof(beneficiaryName), 256);
        PrincipalName = NormalizeRequired(principalName, nameof(principalName), 256);
        CurrentAmount = decimal.Round(currentAmount, 2, MidpointRounding.AwayFromZero);
        CurrencyCode = NormalizeCode(currencyCode, nameof(currencyCode), 3);
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Status = GuaranteeStatus.Active;
        CreatedAtUtc = createdAtUtc;
        ExternalReference = NormalizeOptional(externalReference, 128);

        var registeredEvent = GuaranteeEvent.Registered(Id, createdAtUtc);
        registeredEvent.Guarantee = this;
        Events.Add(registeredEvent);
    }

    private Guarantee()
    {
        GuaranteeNumber = string.Empty;
        BankName = string.Empty;
        BeneficiaryName = string.Empty;
        PrincipalName = string.Empty;
        CurrencyCode = string.Empty;
    }

    public Guid Id { get; private set; }

    public string GuaranteeNumber { get; private set; }

    public string BankName { get; private set; }

    public string BeneficiaryName { get; private set; }

    public string PrincipalName { get; private set; }

    public decimal CurrentAmount { get; private set; }

    public string CurrencyCode { get; private set; }

    public DateOnly IssueDate { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    public GuaranteeStatus Status { get; private set; }

    public string? ExternalReference { get; private set; }

    public string? SupersededByGuaranteeNumber { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastUpdatedAtUtc { get; private set; }

    public ICollection<GuaranteeDocument> Documents { get; private set; } = new List<GuaranteeDocument>();

    public ICollection<GuaranteeRequest> Requests { get; private set; } = new List<GuaranteeRequest>();

    public ICollection<GuaranteeCorrespondence> Correspondence { get; private set; } = new List<GuaranteeCorrespondence>();

    public ICollection<GuaranteeEvent> Events { get; private set; } = new List<GuaranteeEvent>();

    public GuaranteeDocument RegisterScannedDocument(
        GuaranteeDocumentType documentType,
        string fileName,
        string storagePath,
        int pageCount,
        DateTimeOffset capturedAtUtc,
        string? notes = null)
    {
        var document = new GuaranteeDocument(
            Id,
            documentType,
            GuaranteeDocumentSourceType.Scanned,
            fileName,
            storagePath,
            pageCount,
            capturedAtUtc,
            notes)
        {
            Guarantee = this
        };

        Documents.Add(document);
        return document;
    }

    public GuaranteeRequest CreateRequest(
        GuaranteeRequestType requestType,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        ValidateRequestData(requestType, requestedAmount, requestedExpiryDate);

        var request = new GuaranteeRequest(
            Id,
            requestType,
            requestedAmount,
            requestedExpiryDate,
            notes,
            createdAtUtc)
        {
            Guarantee = this
        };

        Requests.Add(request);

        var requestEvent = GuaranteeEvent.RequestRecorded(Id, request.Id, requestType, createdAtUtc);
        requestEvent.Guarantee = this;
        requestEvent.GuaranteeRequest = request;
        Events.Add(requestEvent);

        return request;
    }

    public GuaranteeCorrespondence RegisterCorrespondence(
        Guid? requestId,
        GuaranteeCorrespondenceDirection direction,
        GuaranteeCorrespondenceKind kind,
        string referenceNumber,
        DateOnly letterDate,
        Guid? scannedDocumentId,
        string? notes,
        DateTimeOffset registeredAtUtc)
    {
        var request = requestId.HasValue ? FindRequest(requestId.Value) : null;
        var document = scannedDocumentId.HasValue ? FindDocument(scannedDocumentId.Value) : null;

        var correspondence = new GuaranteeCorrespondence(
            Id,
            requestId,
            direction,
            kind,
            referenceNumber,
            letterDate,
            scannedDocumentId,
            notes,
            registeredAtUtc)
        {
            Guarantee = this,
            GuaranteeRequest = request,
            ScannedDocument = document
        };

        Correspondence.Add(correspondence);
        document?.Correspondence.Add(correspondence);

        if (request is not null)
        {
            request.AttachCorrespondence(correspondence);

            if (direction == GuaranteeCorrespondenceDirection.Outgoing)
            {
                request.MarkSubmittedToBank(registeredAtUtc);
            }
        }

        return correspondence;
    }

    public void ApplyBankConfirmation(
        Guid requestId,
        Guid correspondenceId,
        DateTimeOffset appliedAtUtc,
        DateOnly? confirmedExpiryDate = null,
        decimal? confirmedAmount = null,
        string? replacementGuaranteeNumber = null,
        string? notes = null)
    {
        var request = FindRequest(requestId);
        var correspondence = FindCorrespondence(correspondenceId);

        if (correspondence.Direction != GuaranteeCorrespondenceDirection.Incoming)
        {
            throw new InvalidOperationException("Only incoming bank correspondence can confirm a guarantee change.");
        }

        if (!correspondence.ScannedDocumentId.HasValue)
        {
            throw new InvalidOperationException("A scanned bank document is required before applying the confirmation.");
        }

        decimal? previousAmount = null;
        decimal? newAmount = null;
        DateOnly? previousExpiryDate = null;
        DateOnly? newExpiryDate = null;
        GuaranteeStatus? previousStatus = null;
        GuaranteeStatus? newStatus = null;
        GuaranteeEventType eventType;
        string summary;

        switch (request.RequestType)
        {
            case GuaranteeRequestType.Extend:
                if (!confirmedExpiryDate.HasValue || confirmedExpiryDate.Value <= ExpiryDate)
                {
                    throw new InvalidOperationException("A later expiry date is required to confirm an extension.");
                }

                previousExpiryDate = ExpiryDate;
                ExpiryDate = confirmedExpiryDate.Value;
                newExpiryDate = ExpiryDate;
                eventType = GuaranteeEventType.ExpiryExtended;
                summary = "Guarantee expiry updated based on official bank confirmation.";
                break;

            case GuaranteeRequestType.Reduce:
                if (!confirmedAmount.HasValue || confirmedAmount.Value <= 0 || confirmedAmount.Value >= CurrentAmount)
                {
                    throw new InvalidOperationException("A lower confirmed amount is required to confirm a reduction.");
                }

                previousAmount = CurrentAmount;
                CurrentAmount = decimal.Round(confirmedAmount.Value, 2, MidpointRounding.AwayFromZero);
                newAmount = CurrentAmount;
                eventType = GuaranteeEventType.AmountReduced;
                summary = "Guarantee amount reduced based on official bank confirmation.";
                break;

            case GuaranteeRequestType.Release:
                previousStatus = Status;
                Status = GuaranteeStatus.Released;
                newStatus = Status;
                eventType = GuaranteeEventType.Released;
                summary = "Guarantee released based on official bank confirmation.";
                break;

            case GuaranteeRequestType.ReplaceWithReducedGuarantee:
                ArgumentException.ThrowIfNullOrWhiteSpace(replacementGuaranteeNumber);

                previousStatus = Status;
                Status = GuaranteeStatus.Replaced;
                SupersededByGuaranteeNumber = NormalizeRequired(
                    replacementGuaranteeNumber,
                    nameof(replacementGuaranteeNumber),
                    64);
                newStatus = Status;
                eventType = GuaranteeEventType.Replaced;
                summary = "Guarantee replaced based on official bank confirmation.";
                break;

            case GuaranteeRequestType.VerifyStatus:
                eventType = GuaranteeEventType.StatusConfirmed;
                summary = string.IsNullOrWhiteSpace(notes)
                    ? "Guarantee status confirmed by the bank."
                    : NormalizeRequired(notes, nameof(notes), 512);
                break;

            default:
                throw new InvalidOperationException("Unsupported guarantee request type.");
        }

        LastUpdatedAtUtc = appliedAtUtc;
        request.MarkCompleted(correspondence.Id, appliedAtUtc);
        correspondence.MarkAppliedToGuarantee(appliedAtUtc);

        var guaranteeEvent = GuaranteeEvent.FromConfirmedChange(
            Id,
            request.Id,
            correspondence.Id,
            eventType,
            appliedAtUtc,
            summary,
            previousAmount,
            newAmount,
            previousExpiryDate,
            newExpiryDate,
            previousStatus,
            newStatus);

        guaranteeEvent.Guarantee = this;
        guaranteeEvent.GuaranteeRequest = request;
        guaranteeEvent.GuaranteeCorrespondence = correspondence;
        Events.Add(guaranteeEvent);
    }

    private GuaranteeRequest FindRequest(Guid requestId)
    {
        return Requests.SingleOrDefault(request => request.Id == requestId)
            ?? throw new InvalidOperationException("The request does not belong to the guarantee.");
    }

    private GuaranteeDocument FindDocument(Guid documentId)
    {
        return Documents.SingleOrDefault(document => document.Id == documentId)
            ?? throw new InvalidOperationException("The document does not belong to the guarantee.");
    }

    private GuaranteeCorrespondence FindCorrespondence(Guid correspondenceId)
    {
        return Correspondence.SingleOrDefault(correspondence => correspondence.Id == correspondenceId)
            ?? throw new InvalidOperationException("The correspondence does not belong to the guarantee.");
    }

    private void ValidateRequestData(
        GuaranteeRequestType requestType,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate)
    {
        switch (requestType)
        {
            case GuaranteeRequestType.Extend:
                if (!requestedExpiryDate.HasValue || requestedExpiryDate.Value <= ExpiryDate)
                {
                    throw new InvalidOperationException("An extension request must propose a later expiry date.");
                }
                break;

            case GuaranteeRequestType.Reduce:
            case GuaranteeRequestType.ReplaceWithReducedGuarantee:
                if (!requestedAmount.HasValue || requestedAmount.Value <= 0 || requestedAmount.Value >= CurrentAmount)
                {
                    throw new InvalidOperationException("A reduction request must propose a lower amount.");
                }
                break;
        }
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

    private static string NormalizeCode(string value, string paramName, int length)
    {
        var normalized = NormalizeRequired(value, paramName, length).ToUpperInvariant();

        if (normalized.Length != length)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Value must be exactly {length} characters.");
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

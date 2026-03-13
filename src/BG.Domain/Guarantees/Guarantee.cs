namespace BG.Domain.Guarantees;

public sealed class Guarantee
{
    private Guarantee(
        string guaranteeNumber,
        string bankName,
        string beneficiaryName,
        string principalName,
        GuaranteeCategory category,
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

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category), "Guarantee category must be valid.");
        }

        Id = Guid.NewGuid();
        GuaranteeNumber = NormalizeRequired(guaranteeNumber, nameof(guaranteeNumber), 64);
        BankName = NormalizeRequired(bankName, nameof(bankName), 128);
        BeneficiaryName = NormalizeRequired(beneficiaryName, nameof(beneficiaryName), 256);
        PrincipalName = NormalizeRequired(principalName, nameof(principalName), 256);
        Category = category;
        CurrentAmount = decimal.Round(currentAmount, 2, MidpointRounding.AwayFromZero);
        CurrencyCode = NormalizeCode(currencyCode, nameof(currencyCode), 3);
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Status = GuaranteeStatus.Active;
        CreatedAtUtc = createdAtUtc;
        ExternalReference = NormalizeOptional(externalReference, 128);
    }

    private Guarantee()
    {
        GuaranteeNumber = string.Empty;
        BankName = string.Empty;
        BeneficiaryName = string.Empty;
        PrincipalName = string.Empty;
        CurrencyCode = string.Empty;
        Category = GuaranteeCategory.Contract;
    }

    public Guid Id { get; private set; }

    public string GuaranteeNumber { get; private set; }

    public string BankName { get; private set; }

    public string BeneficiaryName { get; private set; }

    public string PrincipalName { get; private set; }

    public GuaranteeCategory Category { get; private set; }

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

    public static Guarantee RegisterNew(
        string guaranteeNumber,
        string bankName,
        string beneficiaryName,
        string principalName,
        GuaranteeCategory category,
        decimal currentAmount,
        string currencyCode,
        DateOnly issueDate,
        DateOnly expiryDate,
        DateTimeOffset createdAtUtc,
        string? externalReference = null)
    {
        var guarantee = new Guarantee(
            guaranteeNumber,
            bankName,
            beneficiaryName,
            principalName,
            category,
            currentAmount,
            currencyCode,
            issueDate,
            expiryDate,
            createdAtUtc,
            externalReference);

        guarantee.AddEvent(GuaranteeEvent.Registered(guarantee.Id, createdAtUtc));
        return guarantee;
    }

    public GuaranteeDocument RegisterScannedDocument(
        GuaranteeDocumentType documentType,
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
        var sourceType = captureChannel switch
        {
            GuaranteeDocumentCaptureChannel.ManualUpload => GuaranteeDocumentSourceType.Uploaded,
            GuaranteeDocumentCaptureChannel.ScanStation => GuaranteeDocumentSourceType.Scanned,
            GuaranteeDocumentCaptureChannel.OracleImport => GuaranteeDocumentSourceType.Imported,
            _ => GuaranteeDocumentSourceType.Scanned
        };

        var document = new GuaranteeDocument(
            Id,
            documentType,
            sourceType,
            fileName,
            storagePath,
            pageCount,
            capturedAtUtc,
            capturedByUserId,
            capturedByDisplayName,
            captureChannel,
            sourceSystemName,
            sourceReference,
            intakeScenarioKey,
            extractionMethod,
            verifiedDataJson,
            notes)
        {
            Guarantee = this
        };

        Documents.Add(document);
        AddEvent(
            GuaranteeEvent.DocumentCaptured(
                Id,
                document.Id,
                requestId: null,
                documentType,
                captureChannel,
                capturedAtUtc,
                capturedByUserId,
                capturedByDisplayName,
                sourceSystemName,
                sourceReference),
            document: document);
        return document;
    }

    public GuaranteeRequest CreateRequest(
        Guid requestedByUserId,
        GuaranteeRequestType requestType,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate,
        string? notes,
        DateTimeOffset createdAtUtc,
        string? requestedByDisplayName = null,
        GuaranteeRequestChannel requestChannel = GuaranteeRequestChannel.RequestWorkspace)
    {
        ValidateRequestData(requestType, requestedAmount, requestedExpiryDate);

        var request = new GuaranteeRequest(
            Id,
            requestedByUserId,
            requestType,
            requestedAmount,
            requestedExpiryDate,
            notes,
            createdAtUtc,
            requestChannel)
        {
            Guarantee = this
        };

        Requests.Add(request);

        AddEvent(
            GuaranteeEvent.RequestRecorded(
                Id,
                request.Id,
                requestType,
                requestChannel,
                createdAtUtc,
                requestedByUserId,
                requestedByDisplayName),
            request);

        return request;
    }

    public void ReviseRequest(
        Guid requestId,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate,
        string? notes,
        DateTimeOffset revisedAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = FindRequest(requestId);

        if (request.Status is not GuaranteeRequestStatus.Draft and not GuaranteeRequestStatus.Returned)
        {
            throw new InvalidOperationException("Only draft or returned requests can be revised.");
        }

        ValidateRequestData(request.RequestType, requestedAmount, requestedExpiryDate);

        var previousAmount = request.RequestedAmount;
        var previousExpiryDate = request.RequestedExpiryDate;

        request.Revise(requestedAmount, requestedExpiryDate, notes);
        LastUpdatedAtUtc = revisedAtUtc;

        AddEvent(
            GuaranteeEvent.RequestUpdated(
                Id,
                request.Id,
                revisedAtUtc,
                actorUserId,
                actorDisplayName,
                previousAmount,
                request.RequestedAmount,
                previousExpiryDate,
                request.RequestedExpiryDate),
            request);
    }

    public void CancelRequest(
        Guid requestId,
        DateTimeOffset cancelledAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = FindRequest(requestId);
        request.Cancel();
        LastUpdatedAtUtc = cancelledAtUtc;

        AddEvent(
            GuaranteeEvent.RequestCancelled(
                Id,
                request.Id,
                cancelledAtUtc,
                actorUserId,
                actorDisplayName),
            request);
    }

    public void WithdrawRequestFromApproval(
        Guid requestId,
        DateTimeOffset withdrawnAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = FindRequest(requestId);

        if (request.ApprovalProcess is null)
        {
            throw new InvalidOperationException("An approval process is required before a request can be withdrawn.");
        }

        var currentStage = request.ApprovalProcess.GetCurrentStage();
        request.ApprovalProcess.CancelByOwner(withdrawnAtUtc);
        request.WithdrawFromApproval();
        LastUpdatedAtUtc = withdrawnAtUtc;

        AddEvent(
            GuaranteeEvent.RequestWithdrawn(
                Id,
                request.Id,
                withdrawnAtUtc,
                actorUserId,
                actorDisplayName,
                currentStage?.TitleText ?? currentStage?.TitleResourceKey),
            request);
    }

    public void AttachDocumentToRequest(
        Guid requestId,
        Guid documentId,
        DateTimeOffset linkedAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = FindRequest(requestId);
        var document = FindDocument(documentId);

        if (document.DocumentType is not GuaranteeDocumentType.GuaranteeInstrument and not GuaranteeDocumentType.SupportingDocument)
        {
            throw new InvalidOperationException("Only guarantee instruments and supporting documents can be attached to a request.");
        }

        if (request.RequestDocuments.Any(existing => existing.GuaranteeDocumentId == document.Id))
        {
            return;
        }

        var requestDocument = new GuaranteeRequestDocumentLink(
            request.Id,
            document.Id,
            linkedAtUtc,
            actorUserId,
            actorDisplayName)
        {
            GuaranteeRequest = request,
            GuaranteeDocument = document
        };

        request.AttachDocument(requestDocument);
        document.RequestLinks.Add(requestDocument);

        AddEvent(
            GuaranteeEvent.RequestDocumentLinked(
                Id,
                request.Id,
                document.Id,
                document.DocumentType,
                document.FileName,
                linkedAtUtc,
                actorUserId,
                actorDisplayName),
            request,
            document: document);
    }

    public GuaranteeCorrespondence RegisterCorrespondence(
        Guid? requestId,
        GuaranteeCorrespondenceDirection direction,
        GuaranteeCorrespondenceKind kind,
        string referenceNumber,
        DateOnly letterDate,
        Guid? scannedDocumentId,
        string? notes,
        DateTimeOffset registeredAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = requestId.HasValue ? FindRequest(requestId.Value) : null;
        var document = scannedDocumentId.HasValue ? FindDocument(scannedDocumentId.Value) : null;
        ValidateCorrespondenceRegistration(request, direction, kind);

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
        }

        AddEvent(
            GuaranteeEvent.CorrespondenceRecorded(
                Id,
                requestId,
                correspondence.Id,
                direction,
                kind,
                referenceNumber,
                registeredAtUtc,
                actorUserId,
                actorDisplayName),
            request,
            correspondence);

        return correspondence;
    }

    public GuaranteeCorrespondence RecordOutgoingLetterPrint(
        Guid requestId,
        string referenceNumber,
        DateOnly letterDate,
        GuaranteeOutgoingLetterPrintMode printMode,
        DateTimeOffset printedAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null,
        string? note = null)
    {
        var request = FindRequest(requestId);

        if (request.Status is not GuaranteeRequestStatus.ApprovedForDispatch and not GuaranteeRequestStatus.AwaitingBankResponse)
        {
            throw new InvalidOperationException("Only approved or already dispatched requests can have outgoing letters printed.");
        }

        var correspondence = FindOrCreateOutgoingRequestLetter(
            request,
            referenceNumber,
            letterDate,
            note,
            printedAtUtc,
            actorUserId,
            actorDisplayName);

        correspondence.RecordPrint(printMode, printedAtUtc);
        LastUpdatedAtUtc = printedAtUtc;

        AddEvent(
            GuaranteeEvent.OutgoingLetterPrinted(
                Id,
                request.Id,
                correspondence.Id,
                printMode,
                correspondence.PrintCount,
                printedAtUtc,
                actorUserId,
                actorDisplayName),
            request,
            correspondence);

        return correspondence;
    }

    public GuaranteeCorrespondence RecordOutgoingDispatch(
        Guid requestId,
        string referenceNumber,
        DateOnly letterDate,
        GuaranteeDispatchChannel dispatchChannel,
        string? dispatchReference,
        string? dispatchNote,
        DateTimeOffset dispatchedAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = FindRequest(requestId);

        if (request.Status != GuaranteeRequestStatus.ApprovedForDispatch)
        {
            throw new InvalidOperationException("Only requests approved for dispatch can be sent to the bank.");
        }

        var correspondence = FindOrCreateOutgoingRequestLetter(
            request,
            referenceNumber,
            letterDate,
            dispatchNote,
            dispatchedAtUtc,
            actorUserId,
            actorDisplayName);

        correspondence.RecordDispatch(dispatchChannel, dispatchReference, dispatchNote, dispatchedAtUtc);
        request.MarkSubmittedToBank(dispatchedAtUtc);
        LastUpdatedAtUtc = dispatchedAtUtc;

        AddEvent(
            GuaranteeEvent.OutgoingLetterDispatched(
                Id,
                request.Id,
                correspondence.Id,
                dispatchChannel,
                dispatchReference,
                dispatchNote,
                dispatchedAtUtc,
                actorUserId,
                actorDisplayName),
            request,
            correspondence);

        return correspondence;
    }

    public void ConfirmOutgoingDispatchDelivery(
        Guid requestId,
        Guid correspondenceId,
        DateTimeOffset deliveredAtUtc,
        string? deliveryReference = null,
        string? deliveryNote = null,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        var request = FindRequest(requestId);
        var correspondence = FindCorrespondence(correspondenceId);

        if (correspondence.GuaranteeRequestId != request.Id)
        {
            throw new InvalidOperationException("The outgoing correspondence does not belong to the request.");
        }

        correspondence.ConfirmDelivery(deliveryReference, deliveryNote, deliveredAtUtc);
        LastUpdatedAtUtc = deliveredAtUtc;

        AddEvent(
            GuaranteeEvent.OutgoingLetterDelivered(
                Id,
                request.Id,
                correspondence.Id,
                correspondence.DispatchChannel,
                deliveryReference,
                deliveryNote,
                deliveredAtUtc,
                actorUserId,
                actorDisplayName),
            request,
            correspondence);
    }

    public void ReopenOutgoingDispatch(
        Guid requestId,
        Guid correspondenceId,
        DateTimeOffset reopenedAtUtc,
        Guid? actorUserId = null,
        string? actorDisplayName = null,
        string? correctionNote = null)
    {
        var request = FindRequest(requestId);
        var correspondence = FindCorrespondence(correspondenceId);

        if (correspondence.GuaranteeRequestId != request.Id)
        {
            throw new InvalidOperationException("The outgoing correspondence does not belong to the request.");
        }

        var dispatchChannel = correspondence.DispatchChannel;
        var dispatchReference = correspondence.DispatchReference;

        correspondence.ReopenDispatch();
        request.ReopenForDispatch();
        LastUpdatedAtUtc = reopenedAtUtc;

        AddEvent(
            GuaranteeEvent.OutgoingLetterDispatchReopened(
                Id,
                request.Id,
                correspondence.Id,
                dispatchChannel,
                dispatchReference,
                correctionNote,
                reopenedAtUtc,
                actorUserId,
                actorDisplayName),
            request,
            correspondence);
    }

    public void ApplyBankConfirmation(
        Guid requestId,
        Guid correspondenceId,
        DateTimeOffset appliedAtUtc,
        DateOnly? confirmedExpiryDate = null,
        decimal? confirmedAmount = null,
        string? replacementGuaranteeNumber = null,
        string? notes = null,
        Guid? actedByUserId = null,
        string? actedByDisplayName = null,
        string? operationsScenarioTitleResourceKey = null,
        string? operationsLaneResourceKey = null,
        string? operationsMatchConfidenceResourceKey = null,
        int? operationsMatchScore = null,
        string? operationsPolicyResourceKey = null)
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

        if (correspondence.GuaranteeRequestId != request.Id)
        {
            correspondence.LinkToRequest(request);

            if (!request.Correspondence.Contains(correspondence))
            {
                request.AttachCorrespondence(correspondence);
            }

            foreach (var ledgerEntry in Events.Where(entry =>
                         (entry.GuaranteeCorrespondenceId == correspondence.Id ||
                          (correspondence.ScannedDocumentId.HasValue && entry.GuaranteeDocumentId == correspondence.ScannedDocumentId.Value)) &&
                         !entry.GuaranteeRequestId.HasValue))
            {
                ledgerEntry.LinkToRequest(request);
            }
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

        AddEvent(
            GuaranteeEvent.FromConfirmedChange(
                Id,
                request.Id,
                correspondence.Id,
                eventType,
                appliedAtUtc,
                summary,
                actedByUserId,
                actedByDisplayName,
                previousAmount,
                newAmount,
                previousExpiryDate,
                newExpiryDate,
                previousStatus,
                newStatus,
                operationsScenarioTitleResourceKey,
                operationsLaneResourceKey,
                operationsMatchConfidenceResourceKey,
                operationsMatchScore,
                operationsPolicyResourceKey),
            request,
            correspondence);
    }

    internal void RecordRequestSubmittedForApproval(
        Guid requestId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string? stageLabel)
    {
        var request = FindRequest(requestId);
        AddEvent(
            GuaranteeEvent.RequestSubmittedForApproval(
                Id,
                request.Id,
                occurredAtUtc,
                actorUserId,
                actorDisplayName,
                stageLabel),
            request);
    }

    internal void RecordApprovalDecision(
        Guid requestId,
        GuaranteeEventType eventType,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string? responsibleSignerDisplayName,
        string? stageLabel,
        string? approvalPolicyResourceKey,
        ApprovalLedgerExecutionMode approvalExecutionMode,
        string? note)
    {
        var request = FindRequest(requestId);
        AddEvent(
            GuaranteeEvent.ApprovalDecisionRecorded(
                Id,
                request.Id,
                eventType,
                occurredAtUtc,
                actorUserId,
                actorDisplayName,
                responsibleSignerDisplayName,
                stageLabel,
                approvalPolicyResourceKey,
                approvalExecutionMode,
                note),
            request);
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

    private GuaranteeCorrespondence FindOrCreateOutgoingRequestLetter(
        GuaranteeRequest request,
        string referenceNumber,
        DateOnly letterDate,
        string? note,
        DateTimeOffset recordedAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        var correspondence = request.Correspondence
            .Where(existing => existing.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                               existing.Kind == GuaranteeCorrespondenceKind.RequestLetter)
            .OrderByDescending(existing => existing.RegisteredAtUtc)
            .FirstOrDefault();

        if (correspondence is null)
        {
            return RegisterCorrespondence(
                request.Id,
                GuaranteeCorrespondenceDirection.Outgoing,
                GuaranteeCorrespondenceKind.RequestLetter,
                referenceNumber,
                letterDate,
                scannedDocumentId: null,
                note,
                recordedAtUtc,
                actorUserId,
                actorDisplayName);
        }

        correspondence.EnsureMatchesOutgoingReference(referenceNumber, letterDate);
        return correspondence;
    }

    private static void ValidateCorrespondenceRegistration(
        GuaranteeRequest? request,
        GuaranteeCorrespondenceDirection direction,
        GuaranteeCorrespondenceKind kind)
    {
        if (request is null)
        {
            return;
        }

        if (direction == GuaranteeCorrespondenceDirection.Outgoing &&
            kind == GuaranteeCorrespondenceKind.RequestLetter &&
            request.Status is not GuaranteeRequestStatus.ApprovedForDispatch and not GuaranteeRequestStatus.AwaitingBankResponse)
        {
            throw new InvalidOperationException("Only approved or already dispatched requests can register outgoing request letters.");
        }
    }

    private void AddEvent(
        GuaranteeEvent guaranteeEvent,
        GuaranteeRequest? request = null,
        GuaranteeCorrespondence? correspondence = null,
        GuaranteeDocument? document = null)
    {
        guaranteeEvent.Guarantee = this;
        guaranteeEvent.GuaranteeRequest = request;
        guaranteeEvent.GuaranteeCorrespondence = correspondence;
        guaranteeEvent.GuaranteeDocument = document;
        Events.Add(guaranteeEvent);
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

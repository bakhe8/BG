using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.UnitTests.Domain;

public sealed class GuaranteeAggregateTests
{
    private static readonly Guid RequestsUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Recording_outgoing_letter_after_approval_does_not_change_the_guarantee_or_request_status()
    {
        var guarantee = CreateGuarantee();
        var originalExpiry = guarantee.ExpiryDate;
        var originalAmount = guarantee.CurrentAmount;

        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);
        ApproveForDispatch(request);

        guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Outgoing,
            GuaranteeCorrespondenceKind.RequestLetter,
            "LTR-001",
            new DateOnly(2026, 3, 11),
            scannedDocumentId: null,
            notes: "Sent to bank",
            registeredAtUtc: DateTimeOffset.UtcNow);

        Assert.Equal(originalExpiry, guarantee.ExpiryDate);
        Assert.Equal(originalAmount, guarantee.CurrentAmount);
        Assert.Equal(GuaranteeStatus.Active, guarantee.Status);
        Assert.Equal(GuaranteeRequestStatus.ApprovedForDispatch, request.Status);
        Assert.Equal(RequestsUserId, request.RequestedByUserId);
    }

    [Fact]
    public void Outgoing_letter_requires_request_to_be_approved_for_dispatch()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Outgoing,
            GuaranteeCorrespondenceKind.RequestLetter,
            "LTR-001",
            new DateOnly(2026, 3, 11),
            scannedDocumentId: null,
            notes: "Sent to bank",
            registeredAtUtc: DateTimeOffset.UtcNow));
        Assert.Empty(guarantee.Correspondence);
        Assert.Empty(request.Correspondence);
        Assert.Equal(GuaranteeRequestStatus.Draft, request.Status);
    }

    [Fact]
    public void Incoming_bank_confirmation_updates_guarantee_only_after_scanned_document_is_registered()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);
        ApproveForDispatch(request);

        guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Outgoing,
            GuaranteeCorrespondenceKind.RequestLetter,
            "LTR-001",
            new DateOnly(2026, 3, 11),
            scannedDocumentId: null,
            notes: "Sent to bank",
            registeredAtUtc: DateTimeOffset.UtcNow);
        guarantee.RecordOutgoingDispatch(
            request.Id,
            "LTR-001",
            new DateOnly(2026, 3, 11),
            GuaranteeDispatchChannel.Courier,
            "PKG-1",
            "Sent to bank",
            DateTimeOffset.UtcNow);

        var scannedResponse = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "bank-response.pdf",
            "scans/2026/03/bank-response.pdf",
            2,
            DateTimeOffset.UtcNow);

        var response = guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-889",
            new DateOnly(2026, 3, 15),
            scannedResponse.Id,
            notes: "Official confirmation from bank",
            registeredAtUtc: DateTimeOffset.UtcNow);

        guarantee.ApplyBankConfirmation(
            request.Id,
            response.Id,
            DateTimeOffset.UtcNow,
            confirmedExpiryDate: new DateOnly(2027, 6, 30));

        Assert.Equal(new DateOnly(2027, 6, 30), guarantee.ExpiryDate);
        Assert.Equal(GuaranteeRequestStatus.Completed, request.Status);
        Assert.NotNull(response.AppliedToGuaranteeAtUtc);
        Assert.Contains(guarantee.Events, guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.ExpiryExtended);
    }

    [Fact]
    public void Applied_bank_confirmation_can_be_reopened_for_operations_correction()
    {
        var guarantee = CreateGuarantee();
        var originalExpiryDate = guarantee.ExpiryDate;
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);
        ApproveForDispatch(request);

        guarantee.RecordOutgoingDispatch(
            request.Id,
            "LTR-001",
            new DateOnly(2026, 3, 11),
            GuaranteeDispatchChannel.Courier,
            "PKG-1",
            "Sent to bank",
            DateTimeOffset.UtcNow);

        var scannedResponse = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "bank-response.pdf",
            "scans/2026/03/bank-response.pdf",
            2,
            DateTimeOffset.UtcNow);

        var response = guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-889",
            new DateOnly(2026, 3, 15),
            scannedResponse.Id,
            notes: "Official confirmation from bank",
            registeredAtUtc: DateTimeOffset.UtcNow);

        guarantee.ApplyBankConfirmation(
            request.Id,
            response.Id,
            DateTimeOffset.UtcNow,
            confirmedExpiryDate: new DateOnly(2027, 6, 30));

        guarantee.ReopenAppliedBankConfirmation(
            request.Id,
            response.Id,
            DateTimeOffset.UtcNow.AddMinutes(1),
            RequestsUserId,
            "Operations Reviewer",
            "Applied to the wrong request.",
            "IntakeScenario_Extension_Title",
            "OperationsReviewLane_BankConfirmationReview");

        Assert.Equal(originalExpiryDate, guarantee.ExpiryDate);
        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, request.Status);
        Assert.Null(request.CompletedAtUtc);
        Assert.Null(request.CompletionCorrespondenceId);
        Assert.Null(response.AppliedToGuaranteeAtUtc);
        Assert.Contains(
            guarantee.Events,
            guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.BankConfirmationReopened &&
                              guaranteeEvent.GuaranteeRequestId == request.Id);
    }

    [Fact]
    public void Printing_and_dispatching_outgoing_letter_records_distinct_ledger_entries()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);
        ApproveForDispatch(request);

        var correspondence = guarantee.RecordOutgoingLetterPrint(
            request.Id,
            "LTR-9001",
            new DateOnly(2026, 3, 12),
            GuaranteeOutgoingLetterPrintMode.WorkstationPrinter,
            DateTimeOffset.UtcNow);

        guarantee.RecordOutgoingDispatch(
            request.Id,
            "LTR-9001",
            new DateOnly(2026, 3, 12),
            GuaranteeDispatchChannel.HandDelivery,
            "HAND-1",
            "Delivered to courier desk",
            DateTimeOffset.UtcNow);

        guarantee.ConfirmOutgoingDispatchDelivery(
            request.Id,
            correspondence.Id,
            DateTimeOffset.UtcNow.AddMinutes(5),
            "REC-1",
            "Bank received the envelope");

        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, request.Status);
        Assert.Contains(guarantee.Events, guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.OutgoingLetterPrinted);
        Assert.Contains(guarantee.Events, guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.OutgoingLetterDispatched);
        Assert.Contains(guarantee.Events, guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.OutgoingLetterDelivered);
    }

    [Fact]
    public void Dispatched_letter_can_be_reopened_before_delivery_with_ledger_trace()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Dispatch correction",
            createdAtUtc: DateTimeOffset.UtcNow);
        ApproveForDispatch(request);

        var correspondence = guarantee.RecordOutgoingDispatch(
            request.Id,
            "LTR-777",
            new DateOnly(2026, 3, 12),
            GuaranteeDispatchChannel.Courier,
            "PKG-777",
            "Sent in error",
            DateTimeOffset.UtcNow,
            RequestsUserId,
            "Dispatch User");

        guarantee.ReopenOutgoingDispatch(
            request.Id,
            correspondence.Id,
            DateTimeOffset.UtcNow.AddMinutes(1),
            RequestsUserId,
            "Dispatch User",
            "Courier handoff was recorded too early.");

        Assert.Equal(GuaranteeRequestStatus.ApprovedForDispatch, request.Status);
        Assert.Null(request.SubmittedToBankAtUtc);
        Assert.Null(correspondence.DispatchedAtUtc);
        Assert.Null(correspondence.DispatchChannel);
        Assert.Contains(
            guarantee.Events,
            guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.OutgoingLetterDispatchReopened &&
                              guaranteeEvent.GuaranteeRequestId == request.Id);
    }

    [Fact]
    public void Verify_status_response_records_event_without_mutating_amount_or_dates()
    {
        var guarantee = CreateGuarantee();
        var originalExpiry = guarantee.ExpiryDate;
        var originalAmount = guarantee.CurrentAmount;

        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.VerifyStatus,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Bank status verification",
            createdAtUtc: DateTimeOffset.UtcNow);

        var scannedResponse = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "status-response.pdf",
            "scans/2026/03/status-response.pdf",
            1,
            DateTimeOffset.UtcNow);

        var response = guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankStatusReply,
            "BNK-STATUS-01",
            new DateOnly(2026, 3, 18),
            scannedResponse.Id,
            notes: "Status only",
            registeredAtUtc: DateTimeOffset.UtcNow);

        guarantee.ApplyBankConfirmation(
            request.Id,
            response.Id,
            DateTimeOffset.UtcNow,
            notes: "Bank confirmed the guarantee remains active.");

        Assert.Equal(originalExpiry, guarantee.ExpiryDate);
        Assert.Equal(originalAmount, guarantee.CurrentAmount);
        Assert.Equal(GuaranteeStatus.Active, guarantee.Status);
        Assert.Contains(guarantee.Events, guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.StatusConfirmed);
    }

    [Fact]
    public void Supporting_document_can_be_attached_to_request_with_ledger_trace()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Release,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Attach supporting record",
            createdAtUtc: DateTimeOffset.UtcNow);

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.SupportingDocument,
            "support.pdf",
            "scans/2026/03/support.pdf",
            1,
            DateTimeOffset.UtcNow);

        guarantee.AttachDocumentToRequest(request.Id, document.Id, DateTimeOffset.UtcNow, RequestsUserId, "Request User");

        var requestDocument = Assert.Single(request.RequestDocuments);
        Assert.Equal(document.Id, requestDocument.GuaranteeDocumentId);
        Assert.Contains(
            guarantee.Events,
            guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.RequestDocumentLinked &&
                              guaranteeEvent.GuaranteeRequestId == request.Id &&
                              guaranteeEvent.GuaranteeDocumentId == document.Id);
    }

    [Fact]
    public void Returned_request_can_be_revised_with_ledger_trace()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Original note",
            createdAtUtc: DateTimeOffset.UtcNow);

        var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
        process.AddStage(
            Guid.NewGuid(),
            null,
            null,
            "Approver",
            "Approval",
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);
        process.ReturnCurrentStage(RequestsUserId, DateTimeOffset.UtcNow.AddMinutes(1), "Revise it");
        request.MarkReturnedFromApproval();

        guarantee.ReviseRequest(
            request.Id,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 7, 15),
            notes: "Revised note",
            revisedAtUtc: DateTimeOffset.UtcNow.AddMinutes(2),
            actorUserId: RequestsUserId,
            actorDisplayName: "Request User");

        Assert.Equal(GuaranteeRequestStatus.Returned, request.Status);
        Assert.Equal(new DateOnly(2027, 7, 15), request.RequestedExpiryDate);
        Assert.Equal("Revised note", request.Notes);
        Assert.Contains(
            guarantee.Events,
            guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.RequestUpdated &&
                              guaranteeEvent.GuaranteeRequestId == request.Id);
    }

    [Fact]
    public void Draft_request_can_be_cancelled_with_ledger_trace()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Release,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Cancel this request",
            createdAtUtc: DateTimeOffset.UtcNow);

        guarantee.CancelRequest(
            request.Id,
            DateTimeOffset.UtcNow.AddMinutes(1),
            RequestsUserId,
            "Request User");

        Assert.Equal(GuaranteeRequestStatus.Cancelled, request.Status);
        Assert.Contains(
            guarantee.Events,
            guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.RequestCancelled &&
                              guaranteeEvent.GuaranteeRequestId == request.Id);
    }

    [Fact]
    public void In_approval_request_can_be_withdrawn_with_ledger_trace()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Withdraw this request",
            createdAtUtc: DateTimeOffset.UtcNow);

        var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
        process.AddStage(
            Guid.NewGuid(),
            null,
            null,
            "Approver",
            "Approval",
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);

        guarantee.WithdrawRequestFromApproval(
            request.Id,
            DateTimeOffset.UtcNow.AddMinutes(1),
            RequestsUserId,
            "Request User");

        Assert.Equal(GuaranteeRequestStatus.Cancelled, request.Status);
        Assert.Equal(RequestApprovalProcessStatus.Cancelled, request.ApprovalProcess!.Status);
        Assert.All(request.ApprovalProcess.Stages, stage => Assert.NotEqual(RequestApprovalStageStatus.Active, stage.Status));
        Assert.Contains(
            guarantee.Events,
            guaranteeEvent => guaranteeEvent.EventType == GuaranteeEventType.RequestWithdrawn &&
                              guaranteeEvent.GuaranteeRequestId == request.Id);
    }

    [Fact]
    public void Approval_decision_records_structured_governance_context_in_ledger()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            RequestsUserId,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Governance trace",
            createdAtUtc: DateTimeOffset.UtcNow);

        guarantee.RecordApprovalDecision(
            request.Id,
            GuaranteeEventType.ApprovalApproved,
            DateTimeOffset.UtcNow.AddMinutes(1),
            RequestsUserId,
            "Approver",
            "Approver",
            "Guarantees Supervisor",
            "ApprovalGovernancePolicy_DirectActor",
            ApprovalLedgerExecutionMode.Direct,
            "Approved");

        var approvalEvent = Assert.Single(guarantee.Events.Where(guaranteeEvent =>
            guaranteeEvent.EventType == GuaranteeEventType.ApprovalApproved));
        Assert.Equal("Guarantees Supervisor", approvalEvent.ApprovalStageLabel);
        Assert.Equal("ApprovalGovernancePolicy_DirectActor", approvalEvent.ApprovalPolicyResourceKey);
        Assert.Equal(nameof(ApprovalLedgerExecutionMode.Direct), approvalEvent.ApprovalExecutionMode);
        Assert.Equal("Approver", approvalEvent.ApprovalResponsibleSignerDisplayName);
    }

    private static Guarantee CreateGuarantee()
    {
        return Guarantee.RegisterNew(
            "BG-2026-0001",
            "National Bank",
            "KFSHRC",
            "Main Contractor",
            GuaranteeCategory.Contract,
            100000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);
    }

    private static void ApproveForDispatch(GuaranteeRequest request)
    {
        var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
        process.AddStage(
            Guid.NewGuid(),
            null,
            null,
            "Approver",
            "Approval",
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);
        process.ApproveCurrentStage(Guid.NewGuid(), DateTimeOffset.UtcNow, "Approved");
        request.MarkApprovedForDispatch();
    }
}

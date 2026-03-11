using BG.Domain.Guarantees;

namespace BG.UnitTests.Domain;

public sealed class GuaranteeAggregateTests
{
    [Fact]
    public void Creating_request_and_outgoing_letter_does_not_change_the_guarantee()
    {
        var guarantee = CreateGuarantee();
        var originalExpiry = guarantee.ExpiryDate;
        var originalAmount = guarantee.CurrentAmount;

        var request = guarantee.CreateRequest(
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);

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
        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, request.Status);
    }

    [Fact]
    public void Incoming_bank_confirmation_updates_guarantee_only_after_scanned_document_is_registered()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Requesting extension",
            createdAtUtc: DateTimeOffset.UtcNow);

        guarantee.RegisterCorrespondence(
            request.Id,
            GuaranteeCorrespondenceDirection.Outgoing,
            GuaranteeCorrespondenceKind.RequestLetter,
            "LTR-001",
            new DateOnly(2026, 3, 11),
            scannedDocumentId: null,
            notes: "Sent to bank",
            registeredAtUtc: DateTimeOffset.UtcNow);

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
    public void Verify_status_response_records_event_without_mutating_amount_or_dates()
    {
        var guarantee = CreateGuarantee();
        var originalExpiry = guarantee.ExpiryDate;
        var originalAmount = guarantee.CurrentAmount;

        var request = guarantee.CreateRequest(
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

    private static Guarantee CreateGuarantee()
    {
        return new Guarantee(
            "BG-2026-0001",
            "National Bank",
            "KFSHRC",
            "Main Contractor",
            100000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);
    }
}

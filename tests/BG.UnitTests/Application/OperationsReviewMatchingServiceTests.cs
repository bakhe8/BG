using BG.Application.Intake;
using BG.Application.Operations;
using BG.Application.ReferenceData;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Operations;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class OperationsReviewMatchingServiceTests
{
    [Fact]
    public void SuggestMatches_returns_high_confidence_extension_candidate_for_matching_bank_response()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 12, 31),
            notes: "Awaiting extension confirmation",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        AttachRequestSourceDocument(guarantee, request, "snb-instrument.pdf", GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb, "Saudi National Bank");
        ApproveAndDispatchRequest(request, guarantee, "LTR-1001", new DateOnly(2026, 3, 1));

        var bankResponseDocument = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/sample/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: "extension-confirmation",
            extractionMethod: "OCR",
            verifiedDataJson: "{\"documentFormKey\":\"bank-letter-snb\",\"bankName\":\"Saudi National Bank\",\"newExpiryDate\":\"2027-12-31\"}");

        var bankResponse = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-EXT-1",
            new DateOnly(2026, 3, 5),
            bankResponseDocument.Id,
            "Bank extension reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            bankResponseDocument,
            bankResponse,
            "extension-confirmation",
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var service = new OperationsReviewMatchingService();

        var suggestions = service.SuggestMatches(MapItem(item));

        var suggestion = Assert.Single(suggestions);
        Assert.Equal(request.Id, suggestion.RequestId);
        Assert.Equal("OperationsMatchConfidence_High", suggestion.ConfidenceResourceKey);
        Assert.Contains("OperationsMatchReason_ExpiryDateMatches", suggestion.ReasonResourceKeys);
        Assert.Contains("OperationsMatchReason_WaitingForBankResponse", suggestion.ReasonResourceKeys);
        Assert.Contains("OperationsMatchReason_DocumentFormBankAligned", suggestion.ReasonResourceKeys);
        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb, suggestion.RequestDocumentForm?.Key);
    }

    [Fact]
    public void SuggestMatches_returns_empty_for_non_response_review_item()
    {
        var guarantee = CreateGuarantee();
        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.SupportingDocument,
            "attachment.pdf",
            "guarantees/sample/attachment.pdf",
            1,
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence: null,
            "supporting-attachment",
            OperationsReviewItemCategory.SupportingDocumentation);

        var service = new OperationsReviewMatchingService();

        var suggestions = service.SuggestMatches(MapItem(item));

        Assert.Empty(suggestions);
    }

    [Fact]
    public void SuggestMatches_returns_amount_match_reason_for_reduction_confirmation()
    {
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Reduce,
            requestedAmount: 90000m,
            requestedExpiryDate: null,
            notes: "Awaiting reduction confirmation",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        AttachRequestSourceDocument(guarantee, request, "alrajhi-instrument.pdf", GuaranteeDocumentFormKeys.GuaranteeInstrumentAlRajhi, "Al Rajhi Bank");
        ApproveAndDispatchRequest(request, guarantee, "LTR-RED-1", new DateOnly(2026, 3, 1));

        var bankResponseDocument = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "reduction-response.pdf",
            "guarantees/sample/reduction-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: "reduction-confirmation",
            extractionMethod: "OCR",
            verifiedDataJson: "{\"documentFormKey\":\"bank-letter-alrajhi\",\"bankName\":\"Al Rajhi Bank\",\"amount\":\"90000\"}");

        var bankResponse = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-RED-1",
            new DateOnly(2026, 3, 4),
            bankResponseDocument.Id,
            "Bank reduction reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            bankResponseDocument,
            bankResponse,
            "reduction-confirmation",
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var service = new OperationsReviewMatchingService();

        var suggestion = Assert.Single(service.SuggestMatches(MapItem(item)));

        Assert.Equal(request.Id, suggestion.RequestId);
        Assert.Contains("OperationsMatchReason_AmountMatches", suggestion.ReasonResourceKeys);
        Assert.Contains("OperationsMatchReason_DocumentFormBankAligned", suggestion.ReasonResourceKeys);
        Assert.Equal("OperationsMatchConfidence_High", suggestion.ConfidenceResourceKey);
    }

    [Fact]
    public void SuggestMatches_prefers_candidate_with_aligned_bank_form_family()
    {
        var guarantee = CreateGuarantee();
        var alignedRequest = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.VerifyStatus,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Awaiting status verification",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        AttachRequestSourceDocument(guarantee, alignedRequest, "snb-instrument.pdf", GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb, "Saudi National Bank");
        ApproveAndDispatchRequest(alignedRequest, guarantee, "LTR-STAT-1", new DateOnly(2026, 3, 1));

        var mismatchedRequest = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.VerifyStatus,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Awaiting status verification",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-9));
        AttachRequestSourceDocument(guarantee, mismatchedRequest, "riyad-instrument.pdf", GuaranteeDocumentFormKeys.GuaranteeInstrumentRiyad, "Riyad Bank");
        ApproveAndDispatchRequest(mismatchedRequest, guarantee, "LTR-STAT-2", new DateOnly(2026, 3, 2));

        var bankResponseDocument = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "status-response.pdf",
            "guarantees/sample/status-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: "status-verification",
            extractionMethod: "OCR",
            verifiedDataJson: "{\"documentFormKey\":\"bank-letter-snb\",\"bankName\":\"Saudi National Bank\",\"statusStatement\":\"Guarantee remains active.\"}");

        var bankResponse = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankStatusReply,
            "BNK-STAT-1",
            new DateOnly(2026, 3, 4),
            bankResponseDocument.Id,
            "Bank status reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            bankResponseDocument,
            bankResponse,
            IntakeScenarioKeys.StatusVerification,
            OperationsReviewItemCategory.IncomingStatusReply);

        var service = new OperationsReviewMatchingService();

        var suggestions = service.SuggestMatches(MapItem(item));

        Assert.Equal(2, suggestions.Count);
        Assert.Equal(alignedRequest.Id, suggestions[0].RequestId);
        Assert.Contains("OperationsMatchReason_DocumentFormBankAligned", suggestions[0].ReasonResourceKeys);
        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb, suggestions[0].RequestDocumentForm?.Key);
        Assert.Equal(mismatchedRequest.Id, suggestions[1].RequestId);
        Assert.Contains("OperationsMatchReason_DocumentFormBankMismatch", suggestions[1].ReasonResourceKeys);
        Assert.True(suggestions[0].Score > suggestions[1].Score);
    }

    private static Guarantee CreateGuarantee()
    {
        return Guarantee.RegisterNew(
            "BG-2026-8101",
            "National Bank",
            "KFSHRC",
            "Prime Contractor",
            GuaranteeCategory.Contract,
            150000m,
            "SAR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);
    }

    private static void ApproveAndDispatchRequest(
        GuaranteeRequest request,
        Guarantee guarantee,
        string outgoingReference,
        DateOnly outgoingLetterDate)
    {
        var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-8));
        process.AddStage(
            Guid.NewGuid(),
            null,
            null,
            "Approver",
            "Approval",
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);
        process.ApproveCurrentStage(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-7), "Approved");
        request.MarkApprovedForDispatch();

        guarantee.RecordOutgoingDispatch(
            request.Id,
            outgoingReference,
            outgoingLetterDate,
            GuaranteeDispatchChannel.Courier,
            "PKG-MATCH-1",
            "Sent to bank",
            DateTimeOffset.UtcNow.AddDays(-6));
    }

    private static void AttachRequestSourceDocument(
        Guarantee guarantee,
        GuaranteeRequest request,
        string fileName,
        string documentFormKey,
        string bankName)
    {
        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.GuaranteeInstrument,
            fileName,
            $"guarantees/sample/{fileName}",
            1,
            DateTimeOffset.UtcNow.AddDays(-11),
            intakeScenarioKey: IntakeScenarioKeys.NewGuarantee,
            extractionMethod: "OCR",
            verifiedDataJson: $$"""{"documentFormKey":"{{documentFormKey}}","bankName":"{{bankName}}"}""");

        guarantee.AttachDocumentToRequest(request.Id, document.Id, DateTimeOffset.UtcNow.AddDays(-11));
    }

    private static OperationsReviewItem CreateReviewItem(
        Guarantee guarantee,
        GuaranteeDocument document,
        GuaranteeCorrespondence? correspondence,
        string scenarioKey,
        OperationsReviewItemCategory category)
    {
        var item = new OperationsReviewItem(
            guarantee.Id,
            guarantee.GuaranteeNumber,
            document.Id,
            correspondence?.Id,
            scenarioKey,
            category,
            DateTimeOffset.UtcNow);

        typeof(OperationsReviewItem)
            .GetProperty(
                nameof(OperationsReviewItem.Guarantee),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(item, guarantee);

        typeof(OperationsReviewItem)
            .GetProperty(
                nameof(OperationsReviewItem.GuaranteeDocument),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(item, document);

        typeof(OperationsReviewItem)
            .GetProperty(
                nameof(OperationsReviewItem.GuaranteeCorrespondence),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(item, correspondence);

        return item;
    }

    private static OperationsReviewQueueItemReadModel MapItem(OperationsReviewItem item)
    {
        return new OperationsReviewQueueItemReadModel(
            item.Id,
            item.Guarantee.Id,
            item.GuaranteeNumber,
            item.ScenarioKey,
            item.Category,
            item.Status,
            item.GuaranteeDocument.DocumentType,
            item.GuaranteeDocument.FileName,
            item.GuaranteeCorrespondence?.ReferenceNumber,
            item.CreatedAtUtc,
            item.GuaranteeDocument.CapturedAtUtc,
            item.GuaranteeDocument.CapturedByDisplayName,
            item.GuaranteeDocument.CaptureChannel,
            item.GuaranteeDocument.SourceSystemName,
            item.GuaranteeDocument.SourceReference,
            item.GuaranteeDocument.VerifiedDataJson,
            item.GuaranteeCorrespondence?.LetterDate,
            item.CompletedAtUtc,
            item.Guarantee.Requests
                .Select(request =>
                {
                    var primaryDocument = request.RequestDocuments
                        .OrderBy(link => link.GuaranteeDocument.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                        .ThenBy(link => link.LinkedAtUtc)
                        .Select(link => link.GuaranteeDocument)
                        .FirstOrDefault();

                    return new OperationsReviewRequestCandidateReadModel(
                        request.Id,
                        request.RequestType,
                        request.Status,
                        request.RequestedExpiryDate,
                        request.RequestedAmount,
                        request.SubmittedToBankAtUtc,
                        request.Correspondence
                            .Where(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)
                            .OrderByDescending(correspondence => correspondence.RegisteredAtUtc)
                            .Select(correspondence => correspondence.ReferenceNumber)
                            .FirstOrDefault(),
                        primaryDocument?.DocumentType,
                        primaryDocument?.VerifiedDataJson);
                })
                .ToArray());
    }
}

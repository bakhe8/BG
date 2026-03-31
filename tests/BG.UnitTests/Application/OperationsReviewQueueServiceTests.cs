using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Operations;
using BG.Application.ReferenceData;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;

namespace BG.UnitTests.Application;

public sealed class OperationsReviewQueueServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_returns_open_review_items_and_workflow_templates()
    {
        var actor = CreateActor();
        var item = new OperationsReviewItem(
            Guid.NewGuid(),
            "BG-2026-1001",
            Guid.NewGuid(),
            null,
            "new-guarantee",
            OperationsReviewItemCategory.GuaranteeRegistration,
            DateTimeOffset.UtcNow);

        AttachDocument(item, "instrument.pdf", actor);

        var service = new OperationsReviewQueueService(
            new StubOperationsReviewRepository(actor, item),
            new StubWorkflowTemplateService(),
            new StubOperationsReviewMatchingService());

        var snapshot = await service.GetSnapshotAsync(actor.Id);

        Assert.True(snapshot.HasEligibleActor);
        Assert.Equal(actor.Id, snapshot.ActiveActor!.Id);
        Assert.Single(snapshot.Items);
        Assert.Equal("BG-2026-1001", snapshot.Items[0].GuaranteeNumber);
        Assert.Equal("OperationsReviewStatus_Pending", snapshot.Items[0].StatusResourceKey);
        Assert.Equal(actor.DisplayName, snapshot.Items[0].CapturedByDisplayName);
        Assert.Equal(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
            snapshot.Items[0].DocumentForm?.Key);
        Assert.Single(snapshot.Items[0].MatchSuggestions);
        Assert.NotEmpty(snapshot.WorkflowTemplates);
        Assert.Contains(snapshot.WorkflowTemplates, template => template.Key == GuaranteeRequestType.Extend.ToString());
    }

    [Fact]
    public async Task ApplyBankResponseAsync_applies_extension_confirmation_and_completes_review_item()
    {
        var actor = CreateActor();
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 12, 31),
            notes: "Awaiting extension",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        ApproveAndDispatchRequest(request, guarantee, "LTR-EXT-1", new DateOnly(2026, 3, 1));

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/sample/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: "extension-confirmation",
            extractionMethod: "OCR",
            verifiedDataJson: "{\"newExpiryDate\":\"2027-12-31\"}");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-EXT-1",
            new DateOnly(2026, 3, 5),
            document.Id,
            "Extension reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence,
            "extension-confirmation",
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var repository = new StubOperationsReviewRepository(actor, item);
        var service = new OperationsReviewQueueService(
            repository,
            new StubWorkflowTemplateService(),
            new StubOperationsReviewMatchingService());

        var result = await service.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                actor.Id,
                item.Id,
                request.Id,
                ConfirmedExpiryDate: null,
                ConfirmedAmount: null,
                ReplacementGuaranteeNumber: null,
                Note: "Confirmed by operations"));

        Assert.True(result.Succeeded);
        Assert.Equal(new DateOnly(2027, 12, 31), guarantee.ExpiryDate);
        Assert.Equal(GuaranteeRequestStatus.Completed, request.Status);
        Assert.Equal(request.Id, correspondence.GuaranteeRequestId);
        Assert.NotNull(correspondence.AppliedToGuaranteeAtUtc);
        Assert.Equal(OperationsReviewItemStatus.Completed, item.Status);
        var extensionEvent = Assert.Single(
            guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.ExpiryExtended &&
                ledgerEntry.ActorDisplayName == actor.DisplayName));
        Assert.Equal("IntakeScenario_Extension_Title", extensionEvent.OperationsScenarioTitleResourceKey);
        Assert.Equal("OperationsReviewLane_BankConfirmationReview", extensionEvent.OperationsLaneResourceKey);
        Assert.Equal("OperationsMatchConfidence_High", extensionEvent.OperationsMatchConfidenceResourceKey);
        Assert.Equal(91, extensionEvent.OperationsMatchScore);
        Assert.Equal("OperationsLedgerPolicy_MatchedSuggestionApplied", extensionEvent.OperationsPolicyResourceKey);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ApplyBankResponseAsync_applies_reduction_confirmation_and_updates_guarantee_amount()
    {
        var actor = CreateActor();
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Reduce,
            requestedAmount: 90000m,
            requestedExpiryDate: null,
            notes: "Awaiting reduction",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        ApproveAndDispatchRequest(request, guarantee, "LTR-RED-1", new DateOnly(2026, 3, 2));

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "reduction-response.pdf",
            "guarantees/sample/reduction-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: "reduction-confirmation",
            extractionMethod: "OCR",
            verifiedDataJson: "{\"amount\":\"90000\"}");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-RED-1",
            new DateOnly(2026, 3, 7),
            document.Id,
            "Reduction reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence,
            "reduction-confirmation",
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var repository = new StubOperationsReviewRepository(actor, item);
        var service = new OperationsReviewQueueService(
            repository,
            new StubWorkflowTemplateService(),
            new StubOperationsReviewMatchingService());

        var result = await service.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                actor.Id,
                item.Id,
                request.Id,
                ConfirmedExpiryDate: null,
                ConfirmedAmount: null,
                ReplacementGuaranteeNumber: null,
                Note: "Confirmed reduction"));

        Assert.True(result.Succeeded);
        Assert.Equal(90000m, guarantee.CurrentAmount);
        Assert.Equal(GuaranteeRequestStatus.Completed, request.Status);
        Assert.Equal(OperationsReviewItemStatus.Completed, item.Status);
        var reductionEvent = Assert.Single(
            guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.AmountReduced &&
                ledgerEntry.ActorDisplayName == actor.DisplayName));
        Assert.Equal("IntakeScenario_Reduction_Title", reductionEvent.OperationsScenarioTitleResourceKey);
        Assert.Equal("OperationsReviewLane_BankConfirmationReview", reductionEvent.OperationsLaneResourceKey);
        Assert.Equal("OperationsMatchConfidence_High", reductionEvent.OperationsMatchConfidenceResourceKey);
        Assert.Equal(91, reductionEvent.OperationsMatchScore);
        Assert.Equal("OperationsLedgerPolicy_MatchedSuggestionApplied", reductionEvent.OperationsPolicyResourceKey);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ApplyBankResponseAsync_rejects_response_when_bank_profile_conflicts_with_request_source_document()
    {
        var actor = CreateActor();
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 12, 31),
            notes: "Awaiting extension",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        AttachRequestSourceDocument(
            guarantee,
            request,
            "riyad-instrument.pdf",
            GuaranteeDocumentFormKeys.GuaranteeInstrumentRiyad,
            "Riyad Bank");
        ApproveAndDispatchRequest(request, guarantee, "LTR-EXT-2", new DateOnly(2026, 3, 1));

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/sample/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
            extractionMethod: "OCR",
            verifiedDataJson: "{\"documentFormKey\":\"bank-letter-snb\",\"bankName\":\"Saudi National Bank\",\"newExpiryDate\":\"2027-12-31\"}");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-EXT-2",
            new DateOnly(2026, 3, 5),
            document.Id,
            "Extension reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence,
            IntakeScenarioKeys.ExtensionConfirmation,
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var repository = new StubOperationsReviewRepository(actor, item);
        var service = new OperationsReviewQueueService(
            repository,
            new StubWorkflowTemplateService(),
            new OperationsReviewMatchingService());

        var result = await service.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                actor.Id,
                item.Id,
                request.Id,
                ConfirmedExpiryDate: null,
                ConfirmedAmount: null,
                ReplacementGuaranteeNumber: null,
                Note: "Confirmed by operations"));

        Assert.False(result.Succeeded);
        Assert.Equal(OperationsReviewErrorCodes.ResponseBankProfileMismatch, result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, request.Status);
        Assert.Equal(OperationsReviewItemStatus.Pending, item.Status);
        Assert.Empty(guarantee.Events.Where(ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.ExpiryExtended));
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task GetSnapshotAsync_marks_match_suggestion_as_blocked_when_bank_profile_conflicts()
    {
        var actor = CreateActor();
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 12, 31),
            notes: "Awaiting extension",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        AttachRequestSourceDocument(
            guarantee,
            request,
            "riyad-instrument.pdf",
            GuaranteeDocumentFormKeys.GuaranteeInstrumentRiyad,
            "Riyad Bank");
        ApproveAndDispatchRequest(request, guarantee, "LTR-EXT-3", new DateOnly(2026, 3, 1));

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/sample/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
            extractionMethod: "OCR",
            verifiedDataJson: "{\"documentFormKey\":\"bank-letter-snb\",\"bankName\":\"Saudi National Bank\",\"newExpiryDate\":\"2027-12-31\"}");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-EXT-3",
            new DateOnly(2026, 3, 5),
            document.Id,
            "Extension reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence,
            IntakeScenarioKeys.ExtensionConfirmation,
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var service = new OperationsReviewQueueService(
            new StubOperationsReviewRepository(actor, item),
            new StubWorkflowTemplateService(),
            new OperationsReviewMatchingService());

        var snapshot = await service.GetSnapshotAsync(actor.Id);

        var suggestion = Assert.Single(snapshot.Items[0].MatchSuggestions);
        Assert.True(suggestion.IsSelectionBlocked);
        Assert.Equal(OperationsReviewErrorCodes.ResponseBankProfileMismatch, suggestion.BlockingReasonResourceKey);
    }

    [Fact]
    public async Task ReopenAppliedBankResponseAsync_restores_request_guarantee_and_review_item_for_correction()
    {
        var actor = CreateActor();
        var guarantee = CreateGuarantee();
        var originalExpiryDate = guarantee.ExpiryDate;
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 12, 31),
            notes: "Awaiting extension",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        ApproveAndDispatchRequest(request, guarantee, "LTR-EXT-9", new DateOnly(2026, 3, 1));

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/sample/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
            extractionMethod: "OCR",
            verifiedDataJson: "{\"newExpiryDate\":\"2027-12-31\"}");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-EXT-9",
            new DateOnly(2026, 3, 5),
            document.Id,
            "Extension reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence,
            IntakeScenarioKeys.ExtensionConfirmation,
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var repository = new StubOperationsReviewRepository(actor, item);
        var service = new OperationsReviewQueueService(
            repository,
            new StubWorkflowTemplateService(),
            new StubOperationsReviewMatchingService());

        var applyResult = await service.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                actor.Id,
                item.Id,
                request.Id,
                ConfirmedExpiryDate: null,
                ConfirmedAmount: null,
                ReplacementGuaranteeNumber: null,
                Note: "Confirmed by operations"));

        Assert.True(applyResult.Succeeded);
        repository.ResetSaveChangesFlag();

        var reopenResult = await service.ReopenAppliedBankResponseAsync(
            new ReopenAppliedBankResponseCommand(
                actor.Id,
                item.Id,
                "Wrong request mapping."));

        Assert.True(reopenResult.Succeeded);
        Assert.Equal(originalExpiryDate, guarantee.ExpiryDate);
        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, request.Status);
        Assert.Null(request.CompletedAtUtc);
        Assert.Null(request.CompletionCorrespondenceId);
        Assert.Null(correspondence.AppliedToGuaranteeAtUtc);
        Assert.Equal(OperationsReviewItemStatus.Pending, item.Status);
        Assert.Null(item.CompletedAtUtc);
        var reopenEvent = Assert.Single(
            guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.BankConfirmationReopened &&
                ledgerEntry.ActorDisplayName == actor.DisplayName));
        Assert.Equal("IntakeScenario_Extension_Title", reopenEvent.OperationsScenarioTitleResourceKey);
        Assert.Equal("OperationsReviewLane_BankConfirmationReview", reopenEvent.OperationsLaneResourceKey);
        Assert.Equal("OperationsLedgerPolicy_AppliedResponseReopened", reopenEvent.OperationsPolicyResourceKey);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ReopenAppliedBankResponseAsync_requires_correction_note()
    {
        var actor = CreateActor();
        var guarantee = CreateGuarantee();
        var request = guarantee.CreateRequest(
            Guid.NewGuid(),
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 12, 31),
            notes: "Awaiting extension",
            createdAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        ApproveAndDispatchRequest(request, guarantee, "LTR-EXT-10", new DateOnly(2026, 3, 1));

        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/sample/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
            extractionMethod: "OCR",
            verifiedDataJson: "{\"newExpiryDate\":\"2027-12-31\"}");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-EXT-10",
            new DateOnly(2026, 3, 5),
            document.Id,
            "Extension reply",
            DateTimeOffset.UtcNow);

        var item = CreateReviewItem(
            guarantee,
            document,
            correspondence,
            IntakeScenarioKeys.ExtensionConfirmation,
            OperationsReviewItemCategory.IncomingBankConfirmation);

        var repository = new StubOperationsReviewRepository(actor, item);
        var service = new OperationsReviewQueueService(
            repository,
            new StubWorkflowTemplateService(),
            new StubOperationsReviewMatchingService());

        var applyResult = await service.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                actor.Id,
                item.Id,
                request.Id,
                ConfirmedExpiryDate: null,
                ConfirmedAmount: null,
                ReplacementGuaranteeNumber: null,
                Note: "Confirmed by operations"));

        Assert.True(applyResult.Succeeded);

        var reopenResult = await service.ReopenAppliedBankResponseAsync(
            new ReopenAppliedBankResponseCommand(
                actor.Id,
                item.Id,
                "  "));

        Assert.False(reopenResult.Succeeded);
        Assert.Equal(OperationsReviewErrorCodes.ReopenCorrectionNoteRequired, reopenResult.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.Completed, request.Status);
        Assert.Equal(OperationsReviewItemStatus.Completed, item.Status);
    }

    private static Guarantee CreateGuarantee()
    {
        return Guarantee.RegisterNew(
            "BG-2026-1001",
            "National Bank",
            "KFSHRC",
            "Main Contractor",
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
        var process = new BG.Domain.Workflow.RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-8));
        process.AddStage(Guid.NewGuid(), null, null, "Approver", "Approval", requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);
        process.ApproveCurrentStage(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-7), "Approved");
        request.MarkApprovedForDispatch();

        guarantee.RecordOutgoingDispatch(
            request.Id,
            outgoingReference,
            outgoingLetterDate,
            GuaranteeDispatchChannel.Courier,
            "PKG-OPS-1",
            "Sent to bank",
            DateTimeOffset.UtcNow.AddDays(-6));
    }

    private static User CreateActor()
    {
        var role = new Role("Operations Reviewer", "Operations role");
        role.AssignPermissions(
        [
            new Permission("operations.queue.view", "Operations"),
            new Permission("operations.queue.manage", "Operations")
        ]);

        var actor = new User(
            "operations.reviewer",
            "Operations Reviewer",
            "operations.reviewer@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        actor.AssignRoles([role]);
        return actor;
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

    private static void AttachDocument(OperationsReviewItem item, string fileName, User actor)
    {
        typeof(OperationsReviewItem)
            .GetProperty(
                nameof(OperationsReviewItem.GuaranteeDocument),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(item, CreateDocument(fileName, actor));
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

    private static GuaranteeDocument CreateDocument(string fileName, User actor)
    {
        return new GuaranteeDocument(
            Guid.NewGuid(),
            GuaranteeDocumentType.GuaranteeInstrument,
            GuaranteeDocumentSourceType.Scanned,
            fileName,
            $"guarantees/sample/{fileName}",
            1,
            DateTimeOffset.UtcNow,
            actor.Id,
            actor.DisplayName,
            GuaranteeDocumentCaptureChannel.ScanStation,
            "Scan Station",
            "batch-1",
            IntakeScenarioKeys.NewGuarantee,
            "OCR",
            "{\"documentFormKey\":\"guarantee-instrument-snb\",\"bankName\":\"Saudi National Bank\"}");
    }

    private sealed class StubOperationsReviewRepository : IOperationsReviewRepository
    {
        private readonly IReadOnlyList<OperationsReviewItem> _items;
        private readonly IReadOnlyList<User> _actors;

        public StubOperationsReviewRepository(User actor, params OperationsReviewItem[] items)
        {
            _actors = [actor];
            _items = items;
        }

        public Task<IReadOnlyList<User>> ListOperationsActorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors);
        }

        public Task<User?> GetOperationsActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors.SingleOrDefault(actor => actor.Id == userId));
        }

        public Task<OperationsReviewQueuePageReadModel> ListOpenAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var pageItems = _items
                .Where(item => item.Status != OperationsReviewItemStatus.Completed)
                .Select(MapItem)
                .ToArray();

            return Task.FromResult(
                new OperationsReviewQueuePageReadModel(
                    new PagedResult<OperationsReviewQueueItemReadModel>(
                        pageItems,
                        new PageInfoDto(pageNumber, pageSize, pageItems.Length)),
                    new OperationsReviewQueueCountsReadModel(
                        _items.Count(item => item.Status != OperationsReviewItemStatus.Completed),
                        _items.Count(item => item.Status == OperationsReviewItemStatus.Pending),
                        _items.Count(item => item.Status == OperationsReviewItemStatus.Routed))));
        }

        public Task<IReadOnlyList<OperationsReviewRecentItemReadModel>> ListRecentlyCompletedAsync(
            int takeCount,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<OperationsReviewRecentItemReadModel> completedItems = _items
                .Where(item => item.Status == OperationsReviewItemStatus.Completed && item.CompletedAtUtc.HasValue)
                .OrderByDescending(item => item.CompletedAtUtc)
                .Take(takeCount)
                .Select(item => new OperationsReviewRecentItemReadModel(
                    item.Id,
                    item.GuaranteeNumber,
                    item.ScenarioKey,
                    item.CompletedAtUtc!.Value))
                .ToArray();

            return Task.FromResult(completedItems);
        }

        public bool SaveChangesCalled { get; private set; }

        public Task<OperationsReviewItem?> GetOpenItemByIdAsync(Guid reviewItemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == reviewItemId && item.Status != OperationsReviewItemStatus.Completed));
        }

        public Task<OperationsReviewItem?> GetItemByIdAsync(Guid reviewItemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == reviewItemId));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public void ResetSaveChangesFlag()
        {
            SaveChangesCalled = false;
        }

        private static OperationsReviewQueueItemReadModel MapItem(OperationsReviewItem item)
        {
            var guarantee = item.Guarantee;

            return new OperationsReviewQueueItemReadModel(
                item.Id,
                guarantee?.Id ?? Guid.Empty,
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
                guarantee is null
                    ? []
                    : guarantee.Requests
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

    private sealed class StubWorkflowTemplateService : IWorkflowTemplateService
    {
        public Task<IReadOnlyList<RequestWorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RequestWorkflowTemplateDto> templates =
            [
                new RequestWorkflowTemplateDto(
                    GuaranteeRequestType.Extend.ToString(),
                    GuaranteeRequestType.Extend,
                    null,
                    null,
                    "WorkflowTemplate_Extend_Title",
                    "WorkflowTemplate_Extend_Summary",
                    [
                        new RequestWorkflowStageTemplateDto(
                            1,
                            "WorkflowStage_GuaranteesSupervisor_Title",
                            "WorkflowStage_GuaranteesSupervisor_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf")
                    ])
            ];

            return Task.FromResult(templates);
        }

        public async Task<RequestWorkflowTemplateDto?> GetTemplateAsync(
            GuaranteeRequestType requestType,
            GuaranteeCategory? guaranteeCategory,
            CancellationToken cancellationToken = default)
        {
            var templates = await GetTemplatesAsync(cancellationToken);
            return templates.SingleOrDefault(template =>
                template.RequestType == requestType &&
                template.GuaranteeCategory == guaranteeCategory);
        }
    }

    private sealed class StubOperationsReviewMatchingService : IOperationsReviewMatchingService
    {
        public IReadOnlyList<OperationsReviewMatchSuggestionDto> SuggestMatches(OperationsReviewQueueItemReadModel item)
        {
            if (item.CandidateRequests.Count == 0)
            {
                return
                [
                    new OperationsReviewMatchSuggestionDto(
                        Guid.NewGuid(),
                        "RequestType_Extend",
                        "RequestStatus_AwaitingBankResponse",
                        91,
                        "OperationsMatchConfidence_High",
                        DateTimeOffset.UtcNow.AddDays(-2),
                        "LTR-1001",
                        ["OperationsMatchReason_RequestTypeAligned"],
                        null,
                        false,
                        null)
                ];
            }

            return
            [
                new OperationsReviewMatchSuggestionDto(
                    item.CandidateRequests.First().RequestId,
                    "RequestType_Extend",
                    "RequestStatus_AwaitingBankResponse",
                    91,
                    "OperationsMatchConfidence_High",
                    DateTimeOffset.UtcNow.AddDays(-2),
                    "LTR-1001",
                    ["OperationsMatchReason_RequestTypeAligned"],
                    null,
                    false,
                    null)
            ];
        }
    }
}

using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Requests;
using BG.Application.Operations;
using BG.Application.Requests;
using BG.Application.ReferenceData;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Notifications;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class RequestWorkspaceServiceTests
{
    [Fact]
    public async Task GetWorkspaceAsync_returns_only_requests_owned_by_active_actor()
    {
        var actorA = CreateRequestActor("request.a", "Request User A");
        var actorB = CreateRequestActor("request.b", "Request User B");

        var guarantee = CreateGuarantee("BG-2026-4101");
        var instrument = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.GuaranteeInstrument,
            "bg-2026-4101-snb.pdf",
            "guarantees/BG-2026-4101/bg-2026-4101-snb.pdf",
            1,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            actorA.Id,
            actorA.DisplayName,
            GuaranteeDocumentCaptureChannel.ScanStation,
            "Scan Station",
            "batch-req-1",
            IntakeScenarioKeys.NewGuarantee,
            "OCR",
            "{\"documentFormKey\":\"guarantee-instrument-snb\",\"bankName\":\"Saudi National Bank\"}",
            "Instrument");
        var requestA = guarantee.CreateRequest(actorA.Id, GuaranteeRequestType.Extend, null, new DateOnly(2027, 06, 30), "Owned by A", DateTimeOffset.UtcNow, actorA.DisplayName);
        guarantee.AttachDocumentToRequest(requestA.Id, instrument.Id, DateTimeOffset.UtcNow.AddMinutes(-9), actorA.Id, actorA.DisplayName);
        guarantee.CreateRequest(actorB.Id, GuaranteeRequestType.Release, null, null, "Owned by B", DateTimeOffset.UtcNow.AddMinutes(1), actorB.DisplayName);

        var service = new RequestWorkspaceService(
            new StubRequestWorkspaceRepository([actorA, actorB], guarantee),
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var snapshot = await service.GetWorkspaceAsync(actorA.Id);

        Assert.True(snapshot.HasEligibleActor);
        Assert.Equal(actorA.Id, snapshot.ActiveActor!.Id);
        Assert.Equal(2, snapshot.AvailableActors.Count);
        Assert.Single(snapshot.OwnedRequests);
        Assert.Equal("GuaranteeCategory_Contract", snapshot.OwnedRequests[0].GuaranteeCategoryResourceKey);
        Assert.Equal("Owned by A", snapshot.OwnedRequests[0].Notes);
        Assert.Equal(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
            snapshot.OwnedRequests[0].PrimaryDocumentForm?.Key);
        Assert.NotEmpty(snapshot.OwnedRequests[0].LedgerEntries);
        Assert.Equal(actorA.DisplayName, snapshot.OwnedRequests[0].LedgerEntries[0].ActorDisplayName);
        Assert.Equal("RequestsWorkspace_ActorIsolatedNotice", snapshot.ContextNoticeResourceKey);
    }

    [Fact]
    public async Task CreateRequestAsync_allows_same_request_type_for_different_owner()
    {
        var actorA = CreateRequestActor("request.a", "Request User A");
        var actorB = CreateRequestActor("request.b", "Request User B");

        var guarantee = CreateGuarantee("BG-2026-4102");
        guarantee.CreateRequest(actorA.Id, GuaranteeRequestType.Release, null, null, "First request", DateTimeOffset.UtcNow);

        var repository = new StubRequestWorkspaceRepository([actorA, actorB], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var result = await service.CreateRequestAsync(
            new CreateGuaranteeRequestCommand(
                actorB.Id,
                guarantee.GuaranteeNumber,
                GuaranteeRequestType.Release,
                null,
                null,
                "Second owner request"));

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.Equal(2, guarantee.Requests.Count);
        Assert.Contains(guarantee.Requests, request => request.RequestedByUserId == actorB.Id);
        Assert.Contains(
            guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.RequestRecorded &&
                           ledgerEntry.ActorDisplayName == actorB.DisplayName);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateRequestAsync_links_existing_supporting_documents_to_new_request()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4102-A");
        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.SupportingDocument,
            "request-support.pdf",
            "guarantees/BG-2026-4102-A/request-support.pdf",
            1,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            actor.Id,
            actor.DisplayName,
            GuaranteeDocumentCaptureChannel.ManualUpload,
            null,
            null,
            "supporting-attachment",
            "Manual",
            null,
            "Support");

        var repository = new StubRequestWorkspaceRepository([actor], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var result = await service.CreateRequestAsync(
            new CreateGuaranteeRequestCommand(
                actor.Id,
                guarantee.GuaranteeNumber,
                GuaranteeRequestType.Extend,
                null,
                "2027-06-30",
                "Attach support"));

        Assert.True(result.Succeeded, result.ErrorCode);
        var request = Assert.Single(guarantee.Requests);
        var requestDocument = Assert.Single(request.RequestDocuments);
        Assert.Equal(document.Id, requestDocument.GuaranteeDocumentId);
        Assert.Contains(
            guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.RequestDocumentLinked &&
                           ledgerEntry.GuaranteeRequestId == request.Id &&
                           ledgerEntry.GuaranteeDocumentId == document.Id);
    }

    [Fact]
    public async Task CreateRequestAsync_rejects_terminal_guarantee_states()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4102-B");
        typeof(Guarantee)
            .GetProperty(nameof(Guarantee.Status))!
            .SetValue(guarantee, GuaranteeStatus.Released);

        var repository = new StubRequestWorkspaceRepository([actor], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var result = await service.CreateRequestAsync(
            new CreateGuaranteeRequestCommand(
                actor.Id,
                guarantee.GuaranteeNumber,
                GuaranteeRequestType.Release,
                null,
                null,
                "Should fail"));

        Assert.False(result.Succeeded);
        Assert.Equal(RequestErrorCodes.GuaranteeNotRequestable, result.ErrorCode);
    }

    [Fact]
    public async Task GetWorkflowTemplateAsync_returns_release_branch_for_purchase_order_guarantee()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = Guarantee.RegisterNew(
            "BG-2026-4103",
            "National Bank",
            "KFSHRC",
            "Prime Contractor",
            GuaranteeCategory.PurchaseOrder,
            150000m,
            "SAR",
            new DateOnly(2026, 01, 01),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);

        var service = new RequestWorkspaceService(
            new StubRequestWorkspaceRepository([actor], guarantee),
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var template = await service.GetWorkflowTemplateAsync(guarantee.GuaranteeNumber, GuaranteeRequestType.Release);

        Assert.NotNull(template);
        Assert.Equal(GuaranteeCategory.PurchaseOrder, template!.GuaranteeCategory);
        Assert.Equal("GuaranteeCategory_PurchaseOrder", template.GuaranteeCategoryResourceKey);
        Assert.Equal(8, template.Stages.Count);
    }

    [Fact]
    public async Task SubmitRequestForApprovalAsync_creates_process_and_moves_request_into_approval()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4104");
        var request = guarantee.CreateRequest(
            actor.Id,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Submit this",
            createdAtUtc: DateTimeOffset.UtcNow);

        var definition = new RequestWorkflowDefinition(
            "Extend:Contract",
            GuaranteeRequestType.Extend,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Extend_Title",
            "WorkflowTemplate_Extend_Summary",
            DateTimeOffset.UtcNow);
        var role = new Role("Guarantees Supervisor", "Guarantees Supervisor role");
        definition.AddStage(
            role.Id,
            null,
            null,
            "Guarantees Supervisor",
            "Stage summary",
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);

        var repository = new StubRequestWorkspaceRepository([actor], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(definition),
            new StubNotificationService());

        var result = await service.SubmitRequestForApprovalAsync(actor.Id, request.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(GuaranteeRequestStatus.InApproval, request.Status);
        Assert.NotNull(request.ApprovalProcess);
        Assert.Equal(RequestApprovalProcessStatus.InProgress, request.ApprovalProcess!.Status);
        Assert.Equal("Guarantees Supervisor", request.ApprovalProcess.GetCurrentStage()!.TitleText);
        Assert.Contains(
            guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.RequestSubmittedForApproval &&
                           ledgerEntry.ActorDisplayName == actor.DisplayName);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task SubmitRequestForApprovalAsync_rejects_incomplete_workflow_definition()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4104-B");
        var request = guarantee.CreateRequest(
            actor.Id,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Submit this",
            createdAtUtc: DateTimeOffset.UtcNow);

        var invalidDefinition = new RequestWorkflowDefinition(
            "Extend:Contract",
            GuaranteeRequestType.Extend,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Extend_Title",
            "WorkflowTemplate_Extend_Summary",
            DateTimeOffset.UtcNow);
        invalidDefinition.AddStage(
            roleId: null,
            titleResourceKey: "WorkflowStage_GuaranteesSupervisor_Title",
            summaryResourceKey: "WorkflowStage_GuaranteesSupervisor_Summary",
            customTitle: null,
            customSummary: null,
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);

        var service = new RequestWorkspaceService(
            new StubRequestWorkspaceRepository([actor], guarantee),
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(invalidDefinition),
            new StubNotificationService());

        var result = await service.SubmitRequestForApprovalAsync(actor.Id, request.Id);

        Assert.False(result.Succeeded);
        Assert.Equal("RequestsWorkspace_NoWorkflowTemplate", result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.Draft, request.Status);
        Assert.Null(request.ApprovalProcess);
    }

    [Fact]
    public async Task UpdateRequestAsync_revises_returned_request_and_records_ledger()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4104-C");
        var request = guarantee.CreateRequest(
            actor.Id,
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
            "Guarantees Supervisor",
            "Stage summary",
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);
        process.ReturnCurrentStage(actor.Id, DateTimeOffset.UtcNow.AddMinutes(1), "Fix the requested date");
        request.MarkReturnedFromApproval();

        var repository = new StubRequestWorkspaceRepository([actor], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var result = await service.UpdateRequestAsync(
            new UpdateGuaranteeRequestCommand(
                actor.Id,
                request.Id,
                null,
                "2027-07-31",
                "Updated note"));

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.Returned, request.Status);
        Assert.Equal(new DateOnly(2027, 7, 31), request.RequestedExpiryDate);
        Assert.Equal("Updated note", request.Notes);
        Assert.Contains(
            guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.RequestUpdated &&
                           ledgerEntry.GuaranteeRequestId == request.Id &&
                           ledgerEntry.ActorDisplayName == actor.DisplayName);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CancelRequestAsync_cancels_draft_request_and_records_ledger()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4104-D");
        var request = guarantee.CreateRequest(
            actor.Id,
            GuaranteeRequestType.Release,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Cancel me",
            createdAtUtc: DateTimeOffset.UtcNow);

        var repository = new StubRequestWorkspaceRepository([actor], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var result = await service.CancelRequestAsync(actor.Id, request.Id);

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.Cancelled, request.Status);
        Assert.Contains(
            guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.RequestCancelled &&
                           ledgerEntry.GuaranteeRequestId == request.Id &&
                           ledgerEntry.ActorDisplayName == actor.DisplayName);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task WithdrawRequestAsync_withdraws_in_approval_request_and_closes_process()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4104-E");
        var request = guarantee.CreateRequest(
            actor.Id,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Withdraw me",
            createdAtUtc: DateTimeOffset.UtcNow);

        var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
        process.AddStage(
            Guid.NewGuid(),
            null,
            null,
            "Guarantees Supervisor",
            "Stage summary",
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);

        var repository = new StubRequestWorkspaceRepository([actor], guarantee);
        var service = new RequestWorkspaceService(
            repository,
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var result = await service.WithdrawRequestAsync(actor.Id, request.Id);

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.Cancelled, request.Status);
        Assert.Equal(RequestApprovalProcessStatus.Cancelled, request.ApprovalProcess!.Status);
        Assert.Null(request.ApprovalProcess.GetCurrentStage());
        Assert.Contains(
            guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.RequestWithdrawn &&
                           ledgerEntry.GuaranteeRequestId == request.Id &&
                           ledgerEntry.ActorDisplayName == actor.DisplayName);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task GetWorkspaceAsync_maps_approval_ledger_context_to_resource_keys()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4105");
        var request = guarantee.CreateRequest(
            actor.Id,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Approval ledger mapping",
            createdAtUtc: DateTimeOffset.UtcNow,
            actor.DisplayName);

        guarantee.RecordApprovalDecision(
            request.Id,
            GuaranteeEventType.ApprovalApproved,
            DateTimeOffset.UtcNow.AddMinutes(2),
            actor.Id,
            actor.DisplayName,
            actor.DisplayName,
            "Guarantees Supervisor",
            "ApprovalGovernancePolicy_DirectActor",
            ApprovalLedgerExecutionMode.Direct,
            "Approved");

        var service = new RequestWorkspaceService(
            new StubRequestWorkspaceRepository([actor], guarantee),
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var snapshot = await service.GetWorkspaceAsync(actor.Id);

        var requestDto = Assert.Single(snapshot.OwnedRequests);
        var approvalLedgerEntry = Assert.Single(requestDto.LedgerEntries.Where(ledgerEntry =>
            ledgerEntry.ApprovalExecutionModeResourceKey is not null));
        Assert.Equal("Guarantees Supervisor", approvalLedgerEntry.ApprovalStageLabel);
        Assert.Equal("ApprovalGovernancePolicy_DirectActor", approvalLedgerEntry.ApprovalPolicyResourceKey);
        Assert.Equal("ApprovalExecutionMode_Direct", approvalLedgerEntry.ApprovalExecutionModeResourceKey);
        Assert.Equal(actor.DisplayName, approvalLedgerEntry.ApprovalResponsibleSignerDisplayName);
    }

    [Fact]
    public async Task GetWorkspaceAsync_maps_dispatch_and_operations_ledger_context_to_request_timeline()
    {
        var actor = CreateRequestActor("request.a", "Request User A");
        var guarantee = CreateGuarantee("BG-2026-4106");
        var request = guarantee.CreateRequest(
            actor.Id,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Dispatch and operations trace",
            createdAtUtc: DateTimeOffset.UtcNow,
            actor.DisplayName);

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

        guarantee.RecordOutgoingDispatch(
            request.Id,
            "LTR-4106",
            new DateOnly(2026, 3, 12),
            GuaranteeDispatchChannel.Courier,
            "PKG-4106",
            "Sent to bank",
            DateTimeOffset.UtcNow,
            actor.Id,
            actor.DisplayName);

        var responseDocument = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            "extension-response.pdf",
            "guarantees/BG-2026-4106/extension-response.pdf",
            1,
            DateTimeOffset.UtcNow,
            actor.Id,
            actor.DisplayName,
            GuaranteeDocumentCaptureChannel.ManualUpload,
            null,
            null,
            "extension-confirmation",
            "OCR",
            "{\"newExpiryDate\":\"2027-06-30\"}",
            null);

        var response = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "BNK-4106",
            new DateOnly(2026, 3, 15),
            responseDocument.Id,
            "Extension reply",
            DateTimeOffset.UtcNow,
            actor.Id,
            actor.DisplayName);

        guarantee.ApplyBankConfirmation(
            request.Id,
            response.Id,
            DateTimeOffset.UtcNow,
            confirmedExpiryDate: new DateOnly(2027, 6, 30),
            notes: "Applied",
            actedByUserId: actor.Id,
            actedByDisplayName: actor.DisplayName,
            operationsScenarioTitleResourceKey: "IntakeScenario_Extension_Title",
            operationsLaneResourceKey: "OperationsReviewLane_BankConfirmationReview",
            operationsMatchConfidenceResourceKey: "OperationsMatchConfidence_High",
            operationsMatchScore: 91,
            operationsPolicyResourceKey: "OperationsLedgerPolicy_MatchedSuggestionApplied");

        var service = new RequestWorkspaceService(
            new StubRequestWorkspaceRepository([actor], guarantee),
            new StubWorkflowTemplateService(),
            new StubWorkflowDefinitionRepository(),
            new StubNotificationService());

        var snapshot = await service.GetWorkspaceAsync(actor.Id);

        var requestDto = Assert.Single(snapshot.OwnedRequests);
        var dispatchEntry = Assert.Single(requestDto.LedgerEntries.Where(ledgerEntry =>
            ledgerEntry.DispatchStageResourceKey == "DispatchLedgerStep_Dispatched"));
        Assert.Equal("DispatchChannel_Courier", dispatchEntry.DispatchMethodResourceKey);
        Assert.Equal("DispatchLedgerPolicy_BankHandoffRecorded", dispatchEntry.DispatchPolicyResourceKey);

        var operationsEntry = Assert.Single(requestDto.LedgerEntries.Where(ledgerEntry =>
            ledgerEntry.OperationsPolicyResourceKey is not null));
        Assert.Equal("IntakeScenario_Extension_Title", operationsEntry.OperationsScenarioTitleResourceKey);
        Assert.Equal("OperationsReviewLane_BankConfirmationReview", operationsEntry.OperationsLaneResourceKey);
        Assert.Equal("OperationsMatchConfidence_High", operationsEntry.OperationsMatchConfidenceResourceKey);
        Assert.Equal(91, operationsEntry.OperationsMatchScore);
        Assert.Equal("OperationsLedgerPolicy_MatchedSuggestionApplied", operationsEntry.OperationsPolicyResourceKey);
    }

    private static User CreateRequestActor(string username, string displayName)
    {
        return new User(
            username,
            displayName,
            $"{username}@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
    }

    private static Guarantee CreateGuarantee(string guaranteeNumber)
    {
        return Guarantee.RegisterNew(
            guaranteeNumber,
            "National Bank",
            "KFSHRC",
            "Prime Contractor",
            GuaranteeCategory.Contract,
            150000m,
            "SAR",
            new DateOnly(2026, 01, 01),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);
    }

    private sealed class StubRequestWorkspaceRepository : IRequestWorkspaceRepository
    {
        private readonly IReadOnlyList<User> _actors;
        private readonly Guarantee _guarantee;

        public StubRequestWorkspaceRepository(IReadOnlyList<User> actors, Guarantee guarantee)
        {
            _actors = actors;
            _guarantee = guarantee;
        }

        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<User>> ListRequestActorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors);
        }

        public Task<User?> GetRequestActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors.SingleOrDefault(actor => actor.Id == userId));
        }

        public Task<PagedResult<RequestListItemReadModel>> ListOwnedRequestsAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var requests = _guarantee.Requests
                .Where(request => request.RequestedByUserId == userId)
                .Select(request =>
                {
                    var currentStage = request.ApprovalProcess?.GetCurrentStage();
                    return new RequestListItemReadModel(
                        request.Id,
                        request.Guarantee.GuaranteeNumber,
                        request.Guarantee.Category,
                        request.RequestType,
                        request.Status,
                        request.RequestedAmount,
                        request.RequestedExpiryDate,
                        request.Notes,
                        request.CreatedAtUtc,
                        request.Correspondence.Count,
                        currentStage?.TitleResourceKey,
                        currentStage?.TitleText,
                        currentStage?.Role?.Name,
                        request.ApprovalProcess?.LastReturnedNote ?? request.ApprovalProcess?.LastRejectedNote,
                        request.RequestDocuments
                            .Select(link => link.GuaranteeDocument)
                            .OrderBy(document => document.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                            .ThenBy(document => document.CapturedAtUtc)
                            .Select(document => (GuaranteeDocumentType?)document.DocumentType)
                            .FirstOrDefault(),
                        request.RequestDocuments
                            .Select(link => link.GuaranteeDocument)
                            .OrderBy(document => document.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                            .ThenBy(document => document.CapturedAtUtc)
                            .Select(document => document.VerifiedDataJson)
                            .FirstOrDefault(),
                        _guarantee.Events
                            .Where(ledgerEntry => ledgerEntry.GuaranteeRequestId == request.Id)
                            .Select(ledgerEntry => new RequestLedgerEntryReadModel(
                                request.Id,
                                ledgerEntry.Id,
                                ledgerEntry.OccurredAtUtc,
                                ledgerEntry.ActorDisplayName,
                                ledgerEntry.Summary,
                                ledgerEntry.ApprovalStageLabel,
                                ledgerEntry.ApprovalPolicyResourceKey,
                                ledgerEntry.ApprovalResponsibleSignerDisplayName,
                                ledgerEntry.ApprovalExecutionMode,
                                ledgerEntry.DispatchStageResourceKey,
                                ledgerEntry.DispatchMethodResourceKey,
                                ledgerEntry.DispatchPolicyResourceKey,
                                ledgerEntry.OperationsScenarioTitleResourceKey,
                                ledgerEntry.OperationsLaneResourceKey,
                                ledgerEntry.OperationsMatchConfidenceResourceKey,
                                ledgerEntry.OperationsMatchScore,
                                ledgerEntry.OperationsPolicyResourceKey))
                            .ToArray());
                })
                .ToArray();

            return Task.FromResult(
                new PagedResult<RequestListItemReadModel>(
                    requests,
                    new PageInfoDto(pageNumber, pageSize, requests.Length)));
        }

        public Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guarantee?>(_guarantee.GuaranteeNumber == guaranteeNumber ? _guarantee : null);
        }

        public Task<GuaranteeRequest?> GetOwnedRequestByIdAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_guarantee.Requests.SingleOrDefault(request => request.Id == requestId && request.RequestedByUserId == userId));
        }

        public void TrackCreatedRequestGraph(GuaranteeRequest request)
        {
        }

        public void TrackNewApprovalProcessGraph(RequestApprovalProcess approvalProcess)
        {
        }

        public void TrackLedgerEvents(IEnumerable<GuaranteeEvent> ledgerEvents)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
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
                    ]),
                new RequestWorkflowTemplateDto(
                    "Release:PurchaseOrder",
                    GuaranteeRequestType.Release,
                    GuaranteeCategory.PurchaseOrder,
                    "GuaranteeCategory_PurchaseOrder",
                    "WorkflowTemplate_ReleasePurchaseOrder_Title",
                    "WorkflowTemplate_ReleasePurchaseOrder_Summary",
                    [
                        new RequestWorkflowStageTemplateDto(
                            1,
                            "WorkflowStage_GuaranteesSupervisor_Title",
                            "WorkflowStage_GuaranteesSupervisor_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            2,
                            "WorkflowStage_DepartmentManager_Title",
                            "WorkflowStage_DepartmentManager_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            3,
                            "WorkflowStage_ProgramDirector_Title",
                            "WorkflowStage_ProgramDirector_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            4,
                            "WorkflowStage_DeputyFinancialAffairsDirector_Title",
                            "WorkflowStage_DeputyFinancialAffairsDirector_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            5,
                            "WorkflowStage_ProcurementSigner1_Title",
                            "WorkflowStage_ProcurementSigner1_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            6,
                            "WorkflowStage_ProcurementSigner2_Title",
                            "WorkflowStage_ProcurementSigner2_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            7,
                            "WorkflowStage_ProcurementSigner3_Title",
                            "WorkflowStage_ProcurementSigner3_Summary",
                            true,
                            "WorkflowSignatureMode_ButtonStampedPdf",
                            "WorkflowSignatureEffect_FinalLetterPdf"),
                        new RequestWorkflowStageTemplateDto(
                            8,
                            "WorkflowStage_ExecutiveVicePresident_Title",
                            "WorkflowStage_ExecutiveVicePresident_Summary",
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

    private sealed class StubWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        private readonly RequestWorkflowDefinition? _definition;

        public StubWorkflowDefinitionRepository(RequestWorkflowDefinition? definition = null)
        {
            _definition = definition;
        }

        public Task<IReadOnlyList<RequestWorkflowDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RequestWorkflowDefinition>>(_definition is null ? [] : [_definition]);
        }

        public Task<RequestWorkflowDefinition?> GetDefinitionByIdAsync(Guid definitionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_definition?.Id == definitionId ? _definition : null);
        }

        public Task<RequestWorkflowDefinition?> GetDefinitionAsync(
            GuaranteeRequestType requestType,
            GuaranteeCategory? guaranteeCategory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _definition is not null &&
                _definition.RequestType == requestType &&
                _definition.GuaranteeCategory == guaranteeCategory
                    ? _definition
                    : null);
        }

        public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Role>>([]);
        }

        public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Role?>(null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubNotificationService : INotificationService
    {
        public Task SendNotificationAsync(string message, string? link, string requiredPermission, Guid? targetUserId = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, string[] userPermissions, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Notification>>([]);

        public Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

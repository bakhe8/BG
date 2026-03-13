using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Dispatch;
using BG.Application.Models.Dispatch;
using BG.Application.ReferenceData;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class DispatchWorkspaceServiceTests
{
    [Fact]
    public async Task GetWorkspaceAsync_returns_requests_ready_for_dispatch()
    {
        var fixture = DispatchFixture.Create();
        var service = new DispatchWorkspaceService(new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request));

        var snapshot = await service.GetWorkspaceAsync(fixture.Actor.Id);

        Assert.True(snapshot.HasEligibleActor);
        Assert.Equal(fixture.Actor.Id, snapshot.ActiveActor!.Id);
        Assert.Single(snapshot.AvailableActors);
        Assert.Single(snapshot.Items);
        Assert.Empty(snapshot.PendingDeliveryItems);
        Assert.Equal(fixture.Request.Guarantee.GuaranteeNumber, snapshot.Items[0].GuaranteeNumber);
        Assert.Equal("RequestStatus_ApprovedForDispatch", snapshot.Items[0].StatusResourceKey);
        Assert.Equal(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
            snapshot.Items[0].SourceDocumentForm?.Key);
    }

    [Fact]
    public async Task PrintDispatchLetterAsync_records_print_without_moving_request_out_of_ready_for_dispatch()
    {
        var fixture = DispatchFixture.Create();
        var repository = new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request);
        var service = new DispatchWorkspaceService(repository);

        var result = await service.PrintDispatchLetterAsync(
            new PrintDispatchLetterCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9001",
                "2026-03-12",
                GuaranteeOutgoingLetterPrintMode.WorkstationPrinter));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.PrintCount);
        Assert.Equal(GuaranteeRequestStatus.ApprovedForDispatch, fixture.Request.Status);
        Assert.Single(fixture.Request.Correspondence);
        var printEvent = Assert.Single(
            fixture.Request.Guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.OutgoingLetterPrinted &&
                ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName));
        Assert.Equal("DispatchLedgerStep_Printed", printEvent.DispatchStageResourceKey);
        Assert.Equal("DispatchPrintMode_WorkstationPrinter", printEvent.DispatchMethodResourceKey);
        Assert.Equal("DispatchLedgerPolicy_PrintRecordedBeforeExternalDispatch", printEvent.DispatchPolicyResourceKey);
        Assert.True(repository.TrackNewOutgoingCorrespondenceCalled);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task RecordDispatchAsync_registers_outgoing_reference_and_moves_request_to_waiting_for_bank_response()
    {
        var fixture = DispatchFixture.Create();
        var repository = new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request);
        var service = new DispatchWorkspaceService(repository);

        var result = await service.RecordDispatchAsync(
            new RecordDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9001",
                "2026-03-12",
                GuaranteeDispatchChannel.Courier,
                "PKG-1",
                "Sent to bank"));

        Assert.True(result.Succeeded);
        Assert.Equal("LTR-9001", result.Value!.ReferenceNumber);
        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, fixture.Request.Status);
        Assert.NotNull(fixture.Request.SubmittedToBankAtUtc);
        Assert.Single(fixture.Request.Correspondence);
        var dispatchEvent = Assert.Single(
            fixture.Request.Guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.OutgoingLetterDispatched &&
                ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName));
        Assert.Equal("DispatchLedgerStep_Dispatched", dispatchEvent.DispatchStageResourceKey);
        Assert.Equal("DispatchChannel_Courier", dispatchEvent.DispatchMethodResourceKey);
        Assert.Equal("DispatchLedgerPolicy_BankHandoffRecorded", dispatchEvent.DispatchPolicyResourceKey);
        Assert.True(repository.TrackNewOutgoingCorrespondenceCalled);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_records_delivery_confirmation_for_dispatched_letter()
    {
        var fixture = DispatchFixture.Create();
        var repository = new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request);
        var service = new DispatchWorkspaceService(repository);

        await service.RecordDispatchAsync(
            new RecordDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9001",
                "2026-03-12",
                GuaranteeDispatchChannel.HandDelivery,
                "HAND-1",
                "Delivered by courier desk"));

        var correspondence = Assert.Single(fixture.Request.Correspondence);

        var result = await service.ConfirmDeliveryAsync(
            new ConfirmDispatchDeliveryCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                correspondence.Id,
                "REC-1",
                "Bank front desk received it"));

        Assert.True(result.Succeeded);
        Assert.NotNull(correspondence.DeliveredAtUtc);
        var deliveryEvent = Assert.Single(
            fixture.Request.Guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.OutgoingLetterDelivered &&
                ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName));
        Assert.Equal("DispatchLedgerStep_Delivered", deliveryEvent.DispatchStageResourceKey);
        Assert.Equal("DispatchChannel_HandDelivery", deliveryEvent.DispatchMethodResourceKey);
        Assert.Equal("DispatchLedgerPolicy_DeliveryConfirmationRecorded", deliveryEvent.DispatchPolicyResourceKey);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ReopenDispatchAsync_returns_request_to_ready_for_dispatch_and_records_ledger()
    {
        var fixture = DispatchFixture.Create();
        var repository = new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request);
        var service = new DispatchWorkspaceService(repository);

        await service.RecordDispatchAsync(
            new RecordDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9003",
                "2026-03-12",
                GuaranteeDispatchChannel.Courier,
                "PKG-3",
                "Sent too early"));

        var correspondence = Assert.Single(fixture.Request.Correspondence);

        var result = await service.ReopenDispatchAsync(
            new ReopenDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                correspondence.Id,
                "Courier pickup was cancelled."));

        Assert.True(result.Succeeded, result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.ApprovedForDispatch, fixture.Request.Status);
        Assert.Null(fixture.Request.SubmittedToBankAtUtc);
        Assert.Null(correspondence.DispatchedAtUtc);
        var reopenEvent = Assert.Single(
            fixture.Request.Guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.OutgoingLetterDispatchReopened &&
                ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName));
        Assert.Equal("DispatchLedgerStep_Reopened", reopenEvent.DispatchStageResourceKey);
        Assert.Equal("DispatchChannel_Courier", reopenEvent.DispatchMethodResourceKey);
        Assert.Equal("DispatchLedgerPolicy_HandoffCorrectedBeforeDelivery", reopenEvent.DispatchPolicyResourceKey);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ReopenDispatchAsync_requires_correction_note()
    {
        var fixture = DispatchFixture.Create();
        var service = new DispatchWorkspaceService(new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request));

        await service.RecordDispatchAsync(
            new RecordDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9004",
                "2026-03-12",
                GuaranteeDispatchChannel.Courier,
                "PKG-4",
                "Sent too early"));

        var correspondence = Assert.Single(fixture.Request.Correspondence);

        var result = await service.ReopenDispatchAsync(
            new ReopenDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                correspondence.Id,
                " "));

        Assert.False(result.Succeeded);
        Assert.Equal(DispatchErrorCodes.ReopenDispatchNoteRequired, result.ErrorCode);
    }

    [Fact]
    public async Task RecordDispatchAsync_returns_request_not_ready_when_request_is_not_approved_for_dispatch()
    {
        var fixture = DispatchFixture.Create(markApprovedForDispatch: false);
        var service = new DispatchWorkspaceService(new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request));

        var result = await service.RecordDispatchAsync(
            new RecordDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9002",
                "2026-03-12",
                GuaranteeDispatchChannel.Courier,
                null,
                null));

        Assert.False(result.Succeeded);
        Assert.Equal(DispatchErrorCodes.RequestNotReady, result.ErrorCode);
        Assert.Empty(fixture.Request.Correspondence);
    }

    [Fact]
    public async Task PrintDispatchLetterAsync_requires_print_permission()
    {
        var fixture = DispatchFixture.Create(actorPermissionKeys: ["dispatch.view", "dispatch.record"]);
        var service = new DispatchWorkspaceService(new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request));

        var result = await service.PrintDispatchLetterAsync(
            new PrintDispatchLetterCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9010",
                "2026-03-12",
                GuaranteeOutgoingLetterPrintMode.WorkstationPrinter));

        Assert.False(result.Succeeded);
        Assert.Equal(DispatchErrorCodes.DispatchPrintPermissionRequired, result.ErrorCode);
    }

    [Fact]
    public async Task RecordDispatchAsync_requires_email_permission_for_official_email_channel()
    {
        var fixture = DispatchFixture.Create(actorPermissionKeys: ["dispatch.view", "dispatch.record"]);
        var service = new DispatchWorkspaceService(new StubDispatchWorkspaceRepository(fixture.Actor, fixture.Request));

        var result = await service.RecordDispatchAsync(
            new RecordDispatchCommand(
                fixture.Actor.Id,
                fixture.Request.Id,
                "LTR-9011",
                "2026-03-12",
                GuaranteeDispatchChannel.OfficialEmail,
                "MAIL-1",
                "Sent from outlook"));

        Assert.False(result.Succeeded);
        Assert.Equal(DispatchErrorCodes.DispatchEmailPermissionRequired, result.ErrorCode);
    }

    private static Role CreateRole(string name, params string[] permissionKeys)
    {
        var role = new Role(name, $"{name} role");
        role.AssignPermissions(permissionKeys.Select(key => new Permission(key, "Dispatch")));
        return role;
    }

    private static User CreateActor(string username, string displayName, Role role)
    {
        var actor = new User(
            username,
            displayName,
            $"{username}@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        actor.AssignRoles([role]);
        return actor;
    }

    private sealed class StubDispatchWorkspaceRepository : IDispatchWorkspaceRepository
    {
        private readonly User _actor;
        private readonly GuaranteeRequest _request;

        public StubDispatchWorkspaceRepository(User actor, GuaranteeRequest request)
        {
            _actor = actor;
            _request = request;
        }

        public bool SaveChangesCalled { get; private set; }

        public bool TrackNewOutgoingCorrespondenceCalled { get; private set; }

        public Task<IReadOnlyList<User>> ListDispatchActorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>([_actor]);
        }

        public Task<User?> GetDispatchActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(userId == _actor.Id ? _actor : null);
        }

        public Task<PagedResult<DispatchQueueItemReadModel>> ListReadyRequestsAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DispatchQueueItemReadModel> requests = _request.Status == GuaranteeRequestStatus.ApprovedForDispatch
                ? [new DispatchQueueItemReadModel(
                    _request.Id,
                    _request.Guarantee.GuaranteeNumber,
                    _request.Guarantee.Category,
                    _request.RequestType,
                    _request.Status,
                    _request.RequestedByUser.DisplayName,
                    _request.ApprovalProcess?.CompletedAtUtc ?? _request.CreatedAtUtc,
                    _request.Correspondence.FirstOrDefault(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)?.Id,
                    _request.Correspondence.FirstOrDefault(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)?.ReferenceNumber,
                    _request.Correspondence.FirstOrDefault(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)?.LetterDate,
                    _request.RequestDocuments
                        .Select(link => link.GuaranteeDocument)
                        .OrderBy(document => document.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                        .ThenBy(document => document.CapturedAtUtc)
                        .Select(document => (GuaranteeDocumentType?)document.DocumentType)
                        .FirstOrDefault(),
                    _request.RequestDocuments
                        .Select(link => link.GuaranteeDocument)
                        .OrderBy(document => document.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                        .ThenBy(document => document.CapturedAtUtc)
                        .Select(document => document.VerifiedDataJson)
                        .FirstOrDefault(),
                    _request.Correspondence.FirstOrDefault(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)?.PrintCount ?? 0,
                    _request.Correspondence.FirstOrDefault(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)?.LastPrintedAtUtc,
                    _request.Correspondence.FirstOrDefault(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing)?.LastPrintMode)]
                : [];

            return Task.FromResult(
                    new PagedResult<DispatchQueueItemReadModel>(
                        requests,
                        new PageInfoDto(pageNumber, pageSize, requests.Count)));
        }

        public Task<IReadOnlyList<DispatchPendingDeliveryItemReadModel>> ListPendingDeliveryAsync(
            CancellationToken cancellationToken = default)
        {
            var outgoing = _request.Correspondence
                .Where(correspondence =>
                    correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                    correspondence.DispatchedAtUtc.HasValue &&
                    !correspondence.DeliveredAtUtc.HasValue &&
                    correspondence.DispatchChannel.HasValue)
                .Select(correspondence => new DispatchPendingDeliveryItemReadModel(
                    _request.Id,
                    correspondence.Id,
                    _request.Guarantee.GuaranteeNumber,
                    _request.Guarantee.Category,
                    _request.RequestType,
                    _request.RequestedByUser.DisplayName,
                    correspondence.ReferenceNumber,
                    correspondence.LetterDate,
                    _request.RequestDocuments
                        .Select(link => link.GuaranteeDocument)
                        .OrderBy(document => document.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                        .ThenBy(document => document.CapturedAtUtc)
                        .Select(document => (GuaranteeDocumentType?)document.DocumentType)
                        .FirstOrDefault(),
                    _request.RequestDocuments
                        .Select(link => link.GuaranteeDocument)
                        .OrderBy(document => document.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                        .ThenBy(document => document.CapturedAtUtc)
                        .Select(document => document.VerifiedDataJson)
                        .FirstOrDefault(),
                    correspondence.DispatchChannel!.Value,
                    correspondence.DispatchReference,
                    correspondence.DispatchedAtUtc!.Value))
                .ToArray();

            return Task.FromResult<IReadOnlyList<DispatchPendingDeliveryItemReadModel>>(outgoing);
        }

        public Task<GuaranteeRequest?> GetRequestForDispatchAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(requestId == _request.Id ? _request : null);
        }

        public void TrackNewOutgoingCorrespondence(GuaranteeCorrespondence correspondence)
        {
            TrackNewOutgoingCorrespondenceCalled = true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed record DispatchFixture(User Actor, User Requester, GuaranteeRequest Request)
    {
        public static DispatchFixture Create(
            bool markApprovedForDispatch = true,
            string[]? actorPermissionKeys = null)
        {
            var role = CreateRole(
                "Dispatch Officer",
                actorPermissionKeys ?? ["dispatch.view", "dispatch.print", "dispatch.record"]);
            var actor = CreateActor("dispatch.one", "Dispatch One", role);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-7101",
                "National Bank",
                "KFSHRC",
                "Prime Contractor",
                GuaranteeCategory.Contract,
                150000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);

            var request = guarantee.CreateRequest(
                requester.Id,
                GuaranteeRequestType.Extend,
                requestedAmount: null,
                requestedExpiryDate: new DateOnly(2027, 6, 30),
                notes: "Ready for dispatch",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var instrument = guarantee.RegisterScannedDocument(
                GuaranteeDocumentType.GuaranteeInstrument,
                "bg-2026-7101-snb.pdf",
                "guarantees/BG-2026-7101/bg-2026-7101-snb.pdf",
                1,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                requester.Id,
                requester.DisplayName,
                GuaranteeDocumentCaptureChannel.ScanStation,
                "Scan Station",
                "dispatch-batch-1",
                "new-guarantee",
                "OCR",
                "{\"documentFormKey\":\"guarantee-instrument-snb\",\"bankName\":\"Saudi National Bank\"}",
                "Instrument");
            guarantee.AttachDocumentToRequest(request.Id, instrument.Id, DateTimeOffset.UtcNow.AddMinutes(-9), requester.Id, requester.DisplayName);

            if (markApprovedForDispatch)
            {
                var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
                process.AddStage(
                    Guid.NewGuid(),
                    null,
                    null,
                    "Approver",
                    "Approved",
                    requiresLetterSignature: true);
                process.Start();
                request.SubmitForApproval(process);
                process.ApproveCurrentStage(Guid.NewGuid(), DateTimeOffset.UtcNow, "Approved");
                request.MarkApprovedForDispatch();
            }

            return new DispatchFixture(actor, requester, request);
        }
    }
}

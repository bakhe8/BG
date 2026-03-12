using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Models.Approvals;
using BG.Application.Services;
using BG.Application.Approvals;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using Microsoft.Extensions.Options;

namespace BG.UnitTests.Application;

public sealed class ApprovalQueueServiceTests
{
    [Fact]
    public async Task GetWorkspaceAsync_returns_actionable_requests_for_active_actor()
    {
        var fixture = ApprovalFixture.Create();
        var service = CreateService(new StubApprovalQueueRepository(fixture.Actor, fixture.Request));

        var snapshot = await service.GetWorkspaceAsync(fixture.Actor.Id);

        Assert.True(snapshot.HasEligibleActor);
        Assert.Equal(fixture.Actor.Id, snapshot.ActiveActor!.Id);
        Assert.Single(snapshot.AvailableActors);
        Assert.Single(snapshot.Items);
        Assert.Equal(fixture.Request.Guarantee.GuaranteeNumber, snapshot.Items[0].GuaranteeNumber);
        Assert.Equal("RequestChannel_RequestWorkspace", snapshot.Items[0].RequestChannelResourceKey);
        Assert.Equal("RequestStatus_InApproval", snapshot.Items[0].StatusResourceKey);
        Assert.NotEmpty(snapshot.Items[0].Attachments);
        Assert.NotEmpty(snapshot.Items[0].TimelineEntries);
    }

    [Fact]
    public async Task GetDossierAsync_returns_actionable_request_for_active_actor()
    {
        var fixture = ApprovalFixture.Create();
        var service = CreateService(new StubApprovalQueueRepository(fixture.Actor, fixture.Request));

        var snapshot = await service.GetDossierAsync(fixture.Actor.Id, fixture.Request.Id);

        Assert.True(snapshot.HasEligibleActor);
        Assert.Equal(fixture.Actor.Id, snapshot.ActiveActor!.Id);
        Assert.NotNull(snapshot.Item);
        Assert.Equal(fixture.Request.Id, snapshot.Item!.RequestId);
        Assert.NotNull(snapshot.Item.TimelineEntries);
    }

    [Fact]
    public async Task GetDossierAsync_returns_unavailable_when_request_is_not_actionable_for_actor()
    {
        var fixture = ApprovalFixture.Create();
        var otherRole = CreateRole("Other approver", "approvals.queue.view", "approvals.sign");
        var otherActor = CreateActor("other.approver", "Other Approver", otherRole);
        var service = CreateService(new StubApprovalQueueRepository(otherActor, fixture.Request));

        var snapshot = await service.GetDossierAsync(otherActor.Id, fixture.Request.Id);

        Assert.True(snapshot.HasEligibleActor);
        Assert.Equal(otherActor.Id, snapshot.ActiveActor!.Id);
        Assert.Null(snapshot.Item);
        Assert.Equal("ApprovalDossier_RequestNotAvailable", snapshot.UnavailableResourceKey);
    }

    [Fact]
    public async Task ApproveAsync_marks_request_as_approved_for_dispatch_when_last_stage_is_approved()
    {
        var fixture = ApprovalFixture.Create();
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Approved"));

        Assert.True(result.Succeeded);
        Assert.Equal("ApprovalDecision_Approved", result.Value!.OutcomeResourceKey);
        Assert.Equal(GuaranteeRequestStatus.ApprovedForDispatch, fixture.Request.Status);
        Assert.Equal(RequestApprovalProcessStatus.Approved, fixture.Process.Status);
        var approvalEvent = Assert.Single(
            fixture.Request.Guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.ApprovalApproved &&
                ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName));
        Assert.Equal("ApprovalGovernancePolicy_DirectActor", approvalEvent.ApprovalPolicyResourceKey);
        Assert.Equal(nameof(ApprovalLedgerExecutionMode.Direct), approvalEvent.ApprovalExecutionMode);
        Assert.Equal(fixture.Actor.DisplayName, approvalEvent.ApprovalResponsibleSignerDisplayName);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ReturnAsync_marks_request_as_returned()
    {
        var fixture = ApprovalFixture.Create();
        var service = CreateService(new StubApprovalQueueRepository(fixture.Actor, fixture.Request));

        var result = await service.ReturnAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Fix this"));

        Assert.True(result.Succeeded);
        Assert.Equal(GuaranteeRequestStatus.Returned, fixture.Request.Status);
        Assert.Equal(RequestApprovalProcessStatus.Returned, fixture.Process.Status);
        Assert.Equal("Fix this", fixture.Process.LastReturnedNote);
        Assert.Contains(
            fixture.Request.Guarantee.Events,
            ledgerEntry => ledgerEntry.EventType == GuaranteeEventType.ApprovalReturned &&
                           ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName);
    }

    [Fact]
    public async Task RejectAsync_returns_not_actionable_when_actor_has_no_matching_stage_role()
    {
        var fixture = ApprovalFixture.Create();
        var otherRole = CreateRole("Other approver", "approvals.queue.view", "approvals.sign");
        var otherActor = CreateActor("other.approver", "Other Approver", otherRole);
        var service = CreateService(new StubApprovalQueueRepository(otherActor, fixture.Request));

        var result = await service.RejectAsync(new ApprovalDecisionCommand(otherActor.Id, fixture.Request.Id, "Rejected"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Approvals.ApprovalErrorCodes.RequestNotActionable, result.ErrorCode);
        Assert.Equal(GuaranteeRequestStatus.InApproval, fixture.Request.Status);
    }

    [Fact]
    public async Task Delegated_actor_can_approve_and_records_on_behalf_of_audit()
    {
        var fixture = ApprovalFixture.CreateDelegated();
        var delegation = Assert.IsType<ApprovalDelegation>(fixture.Delegation);
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [delegation]);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Approved on behalf"));

        Assert.True(result.Succeeded);
        var stage = Assert.Single(fixture.Process.Stages.Where(currentStage => currentStage.Status == RequestApprovalStageStatus.Approved));
        Assert.Equal(delegation.DelegatorUserId, stage.ActedOnBehalfOfUserId);
        Assert.Equal(delegation.Id, stage.ApprovalDelegationId);
        var approvalEvent = Assert.Single(
            fixture.Request.Guarantee.Events.Where(ledgerEntry =>
                ledgerEntry.EventType == GuaranteeEventType.ApprovalApproved &&
                ledgerEntry.ActorDisplayName == fixture.Actor.DisplayName &&
                ledgerEntry.Summary.Contains(delegation.DelegatorUser.DisplayName, StringComparison.Ordinal)));
        Assert.Equal(nameof(ApprovalLedgerExecutionMode.Delegated), approvalEvent.ApprovalExecutionMode);
        Assert.Equal(delegation.DelegatorUser.DisplayName, approvalEvent.ApprovalResponsibleSignerDisplayName);
        Assert.Equal("ApprovalGovernancePolicy_DefaultDelegationAllowed", approvalEvent.ApprovalPolicyResourceKey);
    }

    [Fact]
    public async Task ApproveAsync_blocks_same_actor_from_signing_multiple_stages_of_the_same_request()
    {
        var fixture = ApprovalFixture.CreateWithRepeatedActorAcrossStages();
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Second approval"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Approvals.ApprovalErrorCodes.GovernancePolicyBlocked, result.ErrorCode);
        Assert.Equal(RequestApprovalProcessStatus.InProgress, fixture.Process.Status);
        Assert.Equal(RequestApprovalStageStatus.Active, fixture.Process.GetCurrentStage()!.Status);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task GetWorkspaceAsync_marks_item_as_governance_blocked_when_same_actor_signed_earlier_stage()
    {
        var fixture = ApprovalFixture.CreateWithRepeatedActorAcrossStages();
        var service = CreateService(new StubApprovalQueueRepository(fixture.Actor, fixture.Request));

        var snapshot = await service.GetWorkspaceAsync(fixture.Actor.Id);

        var item = Assert.Single(snapshot.Items);
        Assert.True(item.Governance.IsDecisionBlocked);
        Assert.Equal("ApprovalQueue_GovernanceConflictSameActor", item.Governance.ReasonResourceKey);
        Assert.Single(item.PriorSignatures);
    }

    [Fact]
    public async Task ApproveAsync_blocks_delegated_stage_when_the_responsible_signer_already_approved_earlier()
    {
        var fixture = ApprovalFixture.CreateDelegatedWithRepeatedResponsibleSigner();
        var delegation = Assert.IsType<ApprovalDelegation>(fixture.Delegation);
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [delegation]);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Delegated second approval"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Approvals.ApprovalErrorCodes.GovernancePolicyBlocked, result.ErrorCode);
        Assert.Equal(RequestApprovalProcessStatus.InProgress, fixture.Process.Status);
        Assert.Equal(RequestApprovalStageStatus.Active, fixture.Process.GetCurrentStage()!.Status);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ApproveAsync_blocks_delegated_release_stage_when_request_type_requires_direct_signer()
    {
        var fixture = ApprovalFixture.CreateDelegatedReleaseStage();
        var delegation = Assert.IsType<ApprovalDelegation>(fixture.Delegation);
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [delegation]);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Delegated release approval"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Approvals.ApprovalErrorCodes.GovernancePolicyBlocked, result.ErrorCode);
        Assert.Equal(RequestApprovalProcessStatus.InProgress, fixture.Process.Status);
        Assert.Equal(RequestApprovalStageStatus.Active, fixture.Process.GetCurrentStage()!.Status);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ApproveAsync_blocks_delegated_final_signature_stage_when_policy_requires_direct_owner()
    {
        var fixture = ApprovalFixture.CreateDelegatedFinalSignatureStage();
        var delegation = Assert.IsType<ApprovalDelegation>(fixture.Delegation);
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [delegation]);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Delegated final approval"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Approvals.ApprovalErrorCodes.GovernancePolicyBlocked, result.ErrorCode);
        Assert.Equal(RequestApprovalProcessStatus.InProgress, fixture.Process.Status);
        Assert.Equal(RequestApprovalStageStatus.Active, fixture.Process.GetCurrentStage()!.Status);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task GetWorkspaceAsync_marks_release_item_as_governance_blocked_when_direct_signer_policy_applies()
    {
        var fixture = ApprovalFixture.CreateDelegatedReleaseStage();
        var service = CreateService(new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [Assert.IsType<ApprovalDelegation>(fixture.Delegation)]));

        var snapshot = await service.GetWorkspaceAsync(fixture.Actor.Id);

        var item = Assert.Single(snapshot.Items);
        Assert.True(item.Governance.IsDecisionBlocked);
        Assert.Equal("ApprovalGovernancePolicy_RequestTypeDirectOnly", item.Governance.PolicyResourceKey);
        Assert.Equal("ApprovalQueue_GovernanceConflictRequestTypeDirectOnly", item.Governance.ReasonResourceKey);
    }

    [Fact]
    public async Task GetWorkspaceAsync_marks_final_signature_item_as_governance_blocked_when_delegated()
    {
        var fixture = ApprovalFixture.CreateDelegatedFinalSignatureStage();
        var service = CreateService(new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [Assert.IsType<ApprovalDelegation>(fixture.Delegation)]));

        var snapshot = await service.GetWorkspaceAsync(fixture.Actor.Id);

        var item = Assert.Single(snapshot.Items);
        Assert.True(item.Governance.IsDecisionBlocked);
        Assert.Equal("ApprovalGovernancePolicy_FinalSignatureDirectOnly", item.Governance.PolicyResourceKey);
        Assert.Equal("ApprovalQueue_GovernanceConflictFinalSignatureDirectOnly", item.Governance.ReasonResourceKey);
    }

    [Fact]
    public async Task ApproveAsync_blocks_delegated_stage_when_amount_threshold_requires_direct_signer()
    {
        var fixture = ApprovalFixture.CreateDelegatedAmountThresholdStage();
        var delegation = Assert.IsType<ApprovalDelegation>(fixture.Delegation);
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [delegation]);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Threshold blocked"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Approvals.ApprovalErrorCodes.GovernancePolicyBlocked, result.ErrorCode);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ApproveAsync_allows_delegated_final_signature_when_workflow_policy_explicitly_allows_it()
    {
        var fixture = ApprovalFixture.CreateDelegatedFinalSignatureStageWithWorkflowOverride();
        var delegation = Assert.IsType<ApprovalDelegation>(fixture.Delegation);
        var repository = new StubApprovalQueueRepository(fixture.Actor, fixture.Request, [delegation]);
        var service = CreateService(repository);

        var result = await service.ApproveAsync(new ApprovalDecisionCommand(fixture.Actor.Id, fixture.Request.Id, "Workflow override"));

        Assert.True(result.Succeeded);
        Assert.True(repository.SaveChangesCalled);
        Assert.Equal(RequestApprovalProcessStatus.Approved, fixture.Process.Status);
    }

    private static ApprovalQueueService CreateService(
        IApprovalQueueRepository repository,
        ApprovalGovernanceOptions? options = null)
    {
        return new ApprovalQueueService(repository, Options.Create(options ?? new ApprovalGovernanceOptions()));
    }

    private static Role CreateRole(string name, params string[] permissionKeys)
    {
        var role = new Role(name, $"{name} role");
        role.AssignPermissions(permissionKeys.Select(key => new Permission(key, "Approvals")));
        return role;
    }

    private static User CreateActor(string username, string displayName, params Role[] roles)
    {
        var actor = new User(
            username,
            displayName,
            $"{username}@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        actor.AssignRoles(roles);
        return actor;
    }

    private sealed class StubApprovalQueueRepository : IApprovalQueueRepository
    {
        private readonly User _actor;
        private readonly GuaranteeRequest _request;
        private readonly IReadOnlyList<ApprovalDelegation> _delegations;

        public StubApprovalQueueRepository(User actor, GuaranteeRequest request, IReadOnlyList<ApprovalDelegation>? delegations = null)
        {
            _actor = actor;
            _request = request;
            _delegations = delegations ?? Array.Empty<ApprovalDelegation>();
        }

        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<User>> ListApprovalActorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>([_actor]);
        }

        public Task<User?> GetApprovalActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(userId == _actor.Id ? _actor : null);
        }

        public Task<IReadOnlyList<ApprovalDelegation>> ListActiveDelegationsAsync(
            Guid delegateUserId,
            DateTimeOffset effectiveAtUtc,
            CancellationToken cancellationToken = default)
        {
            var delegations = _delegations
                .Where(delegation => delegation.DelegateUserId == delegateUserId && delegation.IsActiveAt(effectiveAtUtc))
                .ToArray();

            return Task.FromResult<IReadOnlyList<ApprovalDelegation>>(delegations);
        }

        public Task<PagedResult<ApprovalQueueItemReadModel>> ListActionableRequestsAsync(
            IEnumerable<Guid> actionableRoleIds,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var roleIds = actionableRoleIds.ToHashSet();
            var currentRoleId = _request.ApprovalProcess?.GetCurrentStage()?.RoleId;

            IReadOnlyList<ApprovalQueueItemReadModel> requests = currentRoleId.HasValue && roleIds.Contains(currentRoleId.Value)
                ? [MapRequest(_request)]
                : [];

            return Task.FromResult(
                new PagedResult<ApprovalQueueItemReadModel>(
                    requests,
                    new PageInfoDto(pageNumber, pageSize, requests.Count)));
        }

        public Task<ApprovalQueueItemReadModel?> GetActionableRequestAsync(
            Guid requestId,
            IEnumerable<Guid> actionableRoleIds,
            CancellationToken cancellationToken = default)
        {
            var roleIds = actionableRoleIds.ToHashSet();
            var currentRoleId = _request.ApprovalProcess?.GetCurrentStage()?.RoleId;
            var request = requestId == _request.Id && currentRoleId.HasValue && roleIds.Contains(currentRoleId.Value)
                ? MapRequest(_request)
                : null;

            return Task.FromResult(request);
        }

        public Task<GuaranteeRequest?> GetRequestForApprovalAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(requestId == _request.Id ? _request : null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        private static ApprovalQueueItemReadModel MapRequest(GuaranteeRequest request)
        {
            var currentStage = request.ApprovalProcess!.GetCurrentStage();

            return new ApprovalQueueItemReadModel(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                request.Guarantee.Category,
                request.RequestType,
                request.RequestChannel,
                request.Status,
                request.RequestedByUser.DisplayName,
                request.CreatedAtUtc,
                request.ApprovalProcess.SubmittedAtUtc,
                request.Guarantee.CurrentAmount,
                request.RequestedAmount,
                request.RequestedExpiryDate,
                request.Notes,
                request.ApprovalProcess.Stages.Count,
                currentStage?.Sequence,
                currentStage?.RoleId,
                currentStage?.TitleResourceKey,
                currentStage?.TitleText,
                currentStage?.Role?.Name,
                currentStage?.RequiresLetterSignature ?? false,
                request.ApprovalProcess.Stages
                    .Where(stage =>
                        stage.Status == RequestApprovalStageStatus.Approved &&
                        stage.ActedAtUtc.HasValue)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => new ApprovalPriorSignatureReadModel(
                        stage.Id,
                        stage.Sequence,
                        stage.TitleResourceKey,
                        stage.TitleText,
                        stage.Role?.Name,
                        stage.ActedAtUtc!.Value,
                        stage.ActedByUserId,
                        stage.ActedByUser?.DisplayName,
                        stage.ActedOnBehalfOfUserId ?? stage.ActedByUserId,
                        stage.ActedOnBehalfOfUser?.DisplayName ?? stage.ActedByUser?.DisplayName))
                    .ToArray(),
                request.RequestDocuments
                    .Select(link => new ApprovalRequestAttachmentReadModel(
                        link.Id,
                        link.GuaranteeDocumentId,
                        link.GuaranteeDocument.FileName,
                        link.GuaranteeDocument.DocumentType,
                        link.LinkedAtUtc,
                        link.LinkedByDisplayName,
                        link.GuaranteeDocument.CapturedAtUtc,
                        link.GuaranteeDocument.CapturedByDisplayName,
                        link.GuaranteeDocument.CaptureChannel,
                        link.GuaranteeDocument.SourceSystemName,
                        link.GuaranteeDocument.SourceReference))
                    .ToArray(),
                request.Guarantee.Events
                    .Where(ledgerEntry => ledgerEntry.GuaranteeRequestId == request.Id)
                    .Select(ledgerEntry => new ApprovalRequestTimelineEntryReadModel(
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
                    .ToArray(),
                request.ApprovalProcess.FinalSignatureDelegationPolicy,
                request.ApprovalProcess.DelegationAmountThreshold,
                currentStage?.DelegationPolicy ?? ApprovalDelegationPolicy.Inherit);
        }
    }

    private sealed record ApprovalFixture(
        User Actor,
        User Requester,
        GuaranteeRequest Request,
        RequestApprovalProcess Process,
        ApprovalDelegation? Delegation = null)
    {
        public static ApprovalFixture Create()
        {
            var role = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var actor = CreateActor("approver.one", "Approver One", role);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6101",
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
                notes: "Need approval",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var document = guarantee.RegisterScannedDocument(
                GuaranteeDocumentType.SupportingDocument,
                "request-support.pdf",
                "guarantees/BG-2026-6101/request-support.pdf",
                1,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                requester.Id,
                requester.DisplayName,
                GuaranteeDocumentCaptureChannel.ManualUpload,
                null,
                null,
                "supporting-attachment",
                "Manual",
                null,
                "Supporting note");
            guarantee.AttachDocumentToRequest(request.Id, document.Id, DateTimeOffset.UtcNow.AddMinutes(-9), requester.Id, requester.DisplayName);

            var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
            process.AddStage(
                role.Id,
                "WorkflowStage_GuaranteesSupervisor_Title",
                "WorkflowStage_GuaranteesSupervisor_Summary",
                role.Name,
                "Guarantees Supervisor approval",
                requiresLetterSignature: true);
            process.Start();
            request.SubmitForApproval(process);

            return new ApprovalFixture(actor, requester, request, process);
        }

        public static ApprovalFixture CreateWithRepeatedActorAcrossStages()
        {
            var firstRole = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var secondRole = CreateRole("Department Manager", "approvals.queue.view", "approvals.sign");
            var actor = CreateActor("approver.multi", "Approver Multi", firstRole, secondRole);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6301",
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
                notes: "Repeated signer governance",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
            process.AddStage(firstRole.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", firstRole.Name, "Stage one", true);
            process.AddStage(secondRole.Id, "WorkflowStage_DepartmentManager_Title", "WorkflowStage_DepartmentManager_Summary", secondRole.Name, "Stage two", true);
            process.Start();
            request.SubmitForApproval(process);
            process.ApproveCurrentStage(actor.Id, DateTimeOffset.UtcNow.AddMinutes(-10), "First approval");

            return new ApprovalFixture(actor, requester, request, process);
        }

        public static ApprovalFixture CreateDelegated()
        {
            var firstRole = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var secondRole = CreateRole("Department Manager", "approvals.queue.view", "approvals.sign");
            var delegator = CreateActor("approver.owner", "Approver Owner", firstRole);
            var delegateActor = new User(
                "delegate.actor",
                "Delegate Actor",
                "delegate.actor@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6201",
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
                notes: "Delegated approval",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
            process.AddStage(
                firstRole.Id,
                "WorkflowStage_GuaranteesSupervisor_Title",
                "WorkflowStage_GuaranteesSupervisor_Summary",
                firstRole.Name,
                "Guarantees Supervisor approval",
                requiresLetterSignature: true);
            process.AddStage(
                secondRole.Id,
                "WorkflowStage_DepartmentManager_Title",
                "WorkflowStage_DepartmentManager_Summary",
                secondRole.Name,
                "Department Manager approval",
                requiresLetterSignature: true);
            process.Start();
            request.SubmitForApproval(process);

            var delegation = new ApprovalDelegation(
                delegator.Id,
                delegateActor.Id,
                firstRole.Id,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Annual leave coverage",
                DateTimeOffset.UtcNow.AddDays(-1))
            {
                DelegatorUser = delegator,
                DelegateUser = delegateActor,
                Role = firstRole
            };

            return new ApprovalFixture(delegateActor, requester, request, process, delegation);
        }

        public static ApprovalFixture CreateDelegatedWithRepeatedResponsibleSigner()
        {
            var firstRole = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var secondRole = CreateRole("Department Manager", "approvals.queue.view", "approvals.sign");
            var delegator = CreateActor("approver.owner", "Approver Owner", firstRole, secondRole);
            var delegateActor = new User(
                "delegate.actor",
                "Delegate Actor",
                "delegate.actor@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            delegateActor.AssignRoles([CreateRole("Support Observer", "approvals.queue.view")]);

            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6401",
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
                GuaranteeRequestType.Release,
                requestedAmount: null,
                requestedExpiryDate: null,
                notes: "Delegated repeated responsible signer",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
            process.AddStage(firstRole.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", firstRole.Name, "Stage one", true);
            process.AddStage(secondRole.Id, "WorkflowStage_DepartmentManager_Title", "WorkflowStage_DepartmentManager_Summary", secondRole.Name, "Stage two", true);
            process.Start();
            request.SubmitForApproval(process);
            process.ApproveCurrentStage(delegator.Id, DateTimeOffset.UtcNow.AddMinutes(-15), "First approval");

            var delegation = new ApprovalDelegation(
                delegator.Id,
                delegateActor.Id,
                secondRole.Id,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Annual leave coverage",
                DateTimeOffset.UtcNow.AddDays(-1))
            {
                DelegatorUser = delegator,
                DelegateUser = delegateActor,
                Role = secondRole
            };

            return new ApprovalFixture(delegateActor, requester, request, process, delegation);
        }

        public static ApprovalFixture CreateDelegatedReleaseStage()
        {
            var firstRole = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var secondRole = CreateRole("Department Manager", "approvals.queue.view", "approvals.sign");
            var delegator = CreateActor("release.owner", "Release Owner", firstRole);
            var delegateActor = new User(
                "release.delegate",
                "Release Delegate",
                "release.delegate@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6501",
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
                GuaranteeRequestType.Release,
                requestedAmount: null,
                requestedExpiryDate: null,
                notes: "Release direct signer only",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
            process.AddStage(firstRole.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", firstRole.Name, "Release stage", true);
            process.AddStage(secondRole.Id, "WorkflowStage_DepartmentManager_Title", "WorkflowStage_DepartmentManager_Summary", secondRole.Name, "Second release stage", true);
            process.Start();
            request.SubmitForApproval(process);

            var delegation = new ApprovalDelegation(
                delegator.Id,
                delegateActor.Id,
                firstRole.Id,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Absence coverage",
                DateTimeOffset.UtcNow.AddDays(-1))
            {
                DelegatorUser = delegator,
                DelegateUser = delegateActor,
                Role = firstRole
            };

            return new ApprovalFixture(delegateActor, requester, request, process, delegation);
        }

        public static ApprovalFixture CreateDelegatedFinalSignatureStage()
        {
            var firstRole = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var finalRole = CreateRole("Deputy Director", "approvals.queue.view", "approvals.sign");
            var firstApprover = CreateActor("first.approver", "First Approver", firstRole);
            var finalOwner = CreateActor("final.owner", "Final Owner", finalRole);
            var delegateActor = new User(
                "final.delegate",
                "Final Delegate",
                "final.delegate@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6601",
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
                notes: "Final signature direct only",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
            process.AddStage(firstRole.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", firstRole.Name, "Stage one", true);
            process.AddStage(finalRole.Id, "WorkflowStage_DeputyFinancialAffairsDirector_Title", "WorkflowStage_DeputyFinancialAffairsDirector_Summary", finalRole.Name, "Final stage", true);
            process.Start();
            request.SubmitForApproval(process);
            process.ApproveCurrentStage(firstApprover.Id, DateTimeOffset.UtcNow.AddMinutes(-20), "First approval");

            var delegation = new ApprovalDelegation(
                finalOwner.Id,
                delegateActor.Id,
                finalRole.Id,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Absence coverage",
                DateTimeOffset.UtcNow.AddDays(-1))
            {
                DelegatorUser = finalOwner,
                DelegateUser = delegateActor,
                Role = finalRole
            };

            return new ApprovalFixture(delegateActor, requester, request, process, delegation);
        }

        public static ApprovalFixture CreateDelegatedAmountThresholdStage()
        {
            var ownerRole = CreateRole("Threshold Owner", "approvals.queue.view", "approvals.sign");
            var delegator = CreateActor("threshold.owner", "Threshold Owner", ownerRole);
            var delegateActor = new User(
                "threshold.delegate",
                "Threshold Delegate",
                "threshold.delegate@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6701",
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
                notes: "Amount threshold direct only",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(
                request.Id,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                ApprovalDelegationPolicy.Inherit,
                100000m);
            process.AddStage(ownerRole.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", ownerRole.Name, "Threshold stage", true);
            process.Start();
            request.SubmitForApproval(process);

            var delegation = new ApprovalDelegation(
                delegator.Id,
                delegateActor.Id,
                ownerRole.Id,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Threshold absence coverage",
                DateTimeOffset.UtcNow.AddDays(-1))
            {
                DelegatorUser = delegator,
                DelegateUser = delegateActor,
                Role = ownerRole
            };

            return new ApprovalFixture(delegateActor, requester, request, process, delegation);
        }

        public static ApprovalFixture CreateDelegatedFinalSignatureStageWithWorkflowOverride()
        {
            var firstRole = CreateRole("Guarantees Supervisor", "approvals.queue.view", "approvals.sign");
            var finalRole = CreateRole("Deputy Director", "approvals.queue.view", "approvals.sign");
            var firstApprover = CreateActor("first.approver", "First Approver", firstRole);
            var finalOwner = CreateActor("final.owner", "Final Owner", finalRole);
            var delegateActor = new User(
                "final.delegate",
                "Final Delegate",
                "final.delegate@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            var requester = new User(
                "request.owner",
                "Request Owner",
                "request.owner@bg.local",
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-6801",
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
                notes: "Workflow override on final signature",
                createdAtUtc: DateTimeOffset.UtcNow);
            request.RequestedByUser = requester;

            var process = new RequestApprovalProcess(
                request.Id,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                ApprovalDelegationPolicy.AllowDelegation);
            process.AddStage(firstRole.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", firstRole.Name, "Stage one", true);
            process.AddStage(finalRole.Id, "WorkflowStage_DeputyFinancialAffairsDirector_Title", "WorkflowStage_DeputyFinancialAffairsDirector_Summary", finalRole.Name, "Final stage", true);
            process.Start();
            request.SubmitForApproval(process);
            process.ApproveCurrentStage(firstApprover.Id, DateTimeOffset.UtcNow.AddMinutes(-20), "First approval");

            var delegation = new ApprovalDelegation(
                finalOwner.Id,
                delegateActor.Id,
                finalRole.Id,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(8),
                "Override coverage",
                DateTimeOffset.UtcNow.AddDays(-1))
            {
                DelegatorUser = finalOwner,
                DelegateUser = delegateActor,
                Role = finalRole
            };

            return new ApprovalFixture(delegateActor, requester, request, process, delegation);
        }
    }
}

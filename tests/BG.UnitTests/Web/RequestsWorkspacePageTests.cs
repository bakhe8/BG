using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Documents;
using BG.Application.Models.Requests;
using BG.Application.Operations;
using BG.Domain.Guarantees;
using BG.Web.UI;
using BG.Web.Pages.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class RequestsWorkspacePageTests
{
    [Fact]
    public async Task OnGetAsync_loads_owned_requests_for_active_actor()
    {
        var actorId = Guid.NewGuid();
        var model = new WorkspaceModel(
            new StubRequestWorkspaceService(actorId),
            new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.Snapshot.HasEligibleActor);
        Assert.Equal(actorId, model.Snapshot.ActiveActor!.Id);
        Assert.Single(model.Snapshot.OwnedRequests);
        Assert.Equal(actorId, model.Input.RequestedByUserId);
    }

    [Fact]
    public async Task OnPostAsync_redirects_back_to_actor_workspace_when_request_is_created()
    {
        var actorId = Guid.NewGuid();
        var service = new StubRequestWorkspaceService(actorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer())
        {
            Input = new WorkspaceModel.CreateRequestInput
            {
                RequestedByUserId = actorId,
                GuaranteeNumber = "BG-2026-5001",
                RequestType = GuaranteeRequestType.Extend,
                RequestedExpiryDate = "2027-01-30",
                Notes = "Need extension"
            }
        };

        var result = await model.OnPostAsync(null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Requests/Workspace", redirect.PageName);
        Assert.Equal(actorId, redirect.RouteValues!["actor"]);
        Assert.NotNull(model.StatusMessage);
        Assert.Equal(actorId, service.LastCommand!.RequestedByUserId);
    }

    [Fact]
    public async Task OnPostSubmitAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActorId = Guid.NewGuid();
        var service = new StubRequestWorkspaceService(lockedActorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActorId);

        var result = await model.OnPostSubmitAsync(Guid.NewGuid(), Guid.NewGuid(), null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Requests/Workspace", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastSubmittedByUserId);
    }

    [Fact]
    public async Task OnPostUpdateAsync_redirects_back_to_workspace_for_actor()
    {
        var actorId = Guid.NewGuid();
        var service = new StubRequestWorkspaceService(actorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());
        var requestId = Guid.NewGuid();

        var result = await model.OnPostUpdateAsync(
            requestId,
            actorId,
            null,
            "2027-02-28",
            "Updated note",
            null,
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Requests/Workspace", redirect.PageName);
        Assert.Equal(actorId, redirect.RouteValues!["actor"]);
        Assert.Equal(requestId, service.LastUpdateCommand!.RequestId);
        Assert.Equal(actorId, service.LastUpdateCommand.RequestedByUserId);
        Assert.Equal("RequestsWorkspace_UpdateSuccess", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostCancelAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActorId = Guid.NewGuid();
        var service = new StubRequestWorkspaceService(lockedActorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActorId);
        var requestId = Guid.NewGuid();

        var result = await model.OnPostCancelAsync(requestId, Guid.NewGuid(), null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Requests/Workspace", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastCancelledRequestIdActorId!.Value.ActorId);
        Assert.Equal(requestId, service.LastCancelledRequestIdActorId.Value.RequestId);
    }

    [Fact]
    public async Task OnPostWithdrawAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActorId = Guid.NewGuid();
        var service = new StubRequestWorkspaceService(lockedActorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActorId);
        var requestId = Guid.NewGuid();

        var result = await model.OnPostWithdrawAsync(requestId, Guid.NewGuid(), null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Requests/Workspace", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastWithdrawnRequestIdActorId!.Value.ActorId);
        Assert.Equal(requestId, service.LastWithdrawnRequestIdActorId.Value.RequestId);
    }

    [Fact]
    public async Task ResolveNoOwnerActionReasonResourceKey_returns_completed_reason_for_non_actionable_request()
    {
        var actorId = Guid.NewGuid();
        var model = new WorkspaceModel(
            new StubRequestWorkspaceService(actorId, requestStatusResourceKey: "RequestStatus_Completed"),
            new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.NotNull(model.ActiveRequest);
        Assert.False(model.HasOwnerAction(model.ActiveRequest!));
        Assert.Equal(
            "RequestsWorkspace_NoOwnerAction_Completed",
            model.ResolveNoOwnerActionReasonResourceKey(model.ActiveRequest!));
    }

    private static void AttachAuthenticatedUser(PageModel model, Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ],
            WorkspaceShellDefaults.AuthenticationScheme));

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };
    }

    private sealed class StubRequestWorkspaceService : IRequestWorkspaceService
    {
        private readonly Guid _actorId;

        public StubRequestWorkspaceService(Guid actorId, string requestStatusResourceKey = "RequestStatus_Draft")
        {
            _actorId = actorId;
            _requestStatusResourceKey = requestStatusResourceKey;
        }

        private readonly string _requestStatusResourceKey;

        public CreateGuaranteeRequestCommand? LastCommand { get; private set; }

        public Guid? LastSubmittedByUserId { get; private set; }

        public UpdateGuaranteeRequestCommand? LastUpdateCommand { get; private set; }

        public (Guid ActorId, Guid RequestId)? LastCancelledRequestIdActorId { get; private set; }

        public (Guid ActorId, Guid RequestId)? LastWithdrawnRequestIdActorId { get; private set; }

        public Task<RequestWorkspaceSnapshotDto> GetWorkspaceAsync(
            Guid? requestedActorId,
            int pageNumber = 1,
            CancellationToken cancellationToken = default)
        {
            var actor = new RequestActorSummaryDto(_actorId, "request.user", "Request User");
            var requests =
                new[]
                {
                    new RequestSummaryDto(
                        Guid.NewGuid(),
                        "BG-2026-5001",
                        "GuaranteeCategory_Contract",
                        "RequestType_Extend",
                        _requestStatusResourceKey,
                        null,
                        "2027-01-30",
                        "Need extension",
                        DateTimeOffset.UtcNow,
                        0,
                        null,
                        null,
                        null,
                        _requestStatusResourceKey is "RequestStatus_Draft" or "RequestStatus_Returned",
                        _requestStatusResourceKey == "RequestStatus_Returned",
                        _requestStatusResourceKey is "RequestStatus_Draft" or "RequestStatus_Returned",
                        _requestStatusResourceKey == "RequestStatus_InApproval",
                        false,
                        true,
                        null,
                        null,
                        new GuaranteeDocumentFormSnapshotDto(
                            "guarantee-instrument-snb",
                            "BankProfile_SNB",
                            "DocumentForm_Instrument_SNB_Title",
                            "DocumentForm_Instrument_SNB_Summary"),
                        [
                            new RequestLedgerEntryDto(
                                Guid.NewGuid(),
                                DateTimeOffset.UtcNow,
                                "Request User",
                                "Request recorded: Extend.")
                        ])
                };

            return Task.FromResult(
                new RequestWorkspaceSnapshotDto(
                    actor,
                    [actor],
                    requests,
                    new PageInfoDto(pageNumber, 10, requests.Length),
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
                    ],
                    true,
                    "RequestsWorkspace_ActorIsolatedNotice"));
        }

        public Task<RequestWorkflowTemplateDto?> GetWorkflowTemplateAsync(
            string? guaranteeNumber,
            GuaranteeRequestType requestType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<RequestWorkflowTemplateDto?>(
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
                    ]));
        }

        public Task<OperationResult<CreateGuaranteeRequestReceiptDto>> CreateRequestAsync(
            CreateGuaranteeRequestCommand command,
            CancellationToken cancellationToken = default)
        {
            LastCommand = command;

            return Task.FromResult(
                OperationResult<CreateGuaranteeRequestReceiptDto>.Success(
                    new CreateGuaranteeRequestReceiptDto(
                        Guid.NewGuid(),
                        command.GuaranteeNumber,
                        "RequestType_Extend")));
        }

        public Task<OperationResult<SubmitGuaranteeRequestReceiptDto>> SubmitRequestForApprovalAsync(
            Guid requestedByUserId,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            LastSubmittedByUserId = requestedByUserId;

            return Task.FromResult(
                OperationResult<SubmitGuaranteeRequestReceiptDto>.Success(
                    new SubmitGuaranteeRequestReceiptDto(
                        requestId,
                        "WorkflowStage_GuaranteesSupervisor_Title",
                        "Guarantees Supervisor",
                        "Guarantees Supervisor")));
        }

        public Task<OperationResult<UpdateGuaranteeRequestReceiptDto>> UpdateRequestAsync(
            UpdateGuaranteeRequestCommand command,
            CancellationToken cancellationToken = default)
        {
            LastUpdateCommand = command;
            return Task.FromResult(
                OperationResult<UpdateGuaranteeRequestReceiptDto>.Success(
                    new UpdateGuaranteeRequestReceiptDto(
                        command.RequestId,
                        "BG-2026-5001")));
        }

        public Task<OperationResult<CancelGuaranteeRequestReceiptDto>> CancelRequestAsync(
            Guid requestedByUserId,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            LastCancelledRequestIdActorId = (requestedByUserId, requestId);
            return Task.FromResult(
                OperationResult<CancelGuaranteeRequestReceiptDto>.Success(
                    new CancelGuaranteeRequestReceiptDto(
                        requestId,
                        "BG-2026-5001")));
        }

        public Task<OperationResult<WithdrawGuaranteeRequestReceiptDto>> WithdrawRequestAsync(
            Guid requestedByUserId,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            LastWithdrawnRequestIdActorId = (requestedByUserId, requestId);
            return Task.FromResult(
                OperationResult<WithdrawGuaranteeRequestReceiptDto>.Success(
                    new WithdrawGuaranteeRequestReceiptDto(
                        requestId,
                        "BG-2026-5001")));
        }
    }

    private sealed class PassThroughLocalizer : IStringLocalizer<BG.Web.Localization.SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Array.Empty<LocalizedString>();
        }

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture)
        {
            return this;
        }
    }
}

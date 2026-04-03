using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Application.Models.Documents;
using BG.Web.Localization;
using BG.Web.Pages.Approvals;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class ApprovalQueuePageTests
{
    [Fact]
    public async Task OnGetAsync_loads_queue_for_active_actor()
    {
        var actorId = Guid.NewGuid();
        var model = new QueueModel(new StubApprovalQueueService(actorId), new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.Snapshot.HasEligibleActor);
        Assert.Equal(actorId, model.Snapshot.ActiveActor!.Id);
        Assert.Single(model.Snapshot.Items);
    }

    [Fact]
    public async Task OnPostApproveAsync_redirects_back_to_queue_for_actor()
    {
        var actorId = Guid.NewGuid();
        var service = new StubApprovalQueueService(actorId);
        var model = new QueueModel(service, new PassThroughLocalizer());

        var result = await model.OnPostApproveAsync(actorId, Guid.NewGuid(), "Approved", null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Approvals/Queue", redirect.PageName);
        Assert.Equal(actorId, redirect.RouteValues!["actor"]);
        Assert.Equal(actorId, service.LastCommand!.ApproverUserId);
        Assert.Equal("ApprovalQueue_DecisionSuccess", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostApproveAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActorId = Guid.NewGuid();
        var service = new StubApprovalQueueService(lockedActorId);
        var model = new QueueModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActorId);

        var result = await model.OnPostApproveAsync(Guid.NewGuid(), Guid.NewGuid(), "Approved", null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Approvals/Queue", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastCommand!.ApproverUserId);
    }

    [Fact]
    public async Task OnPostApproveAsync_stays_on_page_when_governance_blocks_the_decision()
    {
        var actorId = Guid.NewGuid();
        var service = new StubApprovalQueueService(actorId)
        {
            ApproveResult = OperationResult<ApprovalDecisionReceiptDto>.Failure("approvals.governance_policy_blocked")
        };
        var model = new QueueModel(service, new PassThroughLocalizer());

        var result = await model.OnPostApproveAsync(actorId, Guid.NewGuid(), "Approved", null, null, CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Null(model.StatusMessage);
        Assert.Equal(actorId, service.LastCommand!.ApproverUserId);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(
            model.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == "approvals.governance_policy_blocked");
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

    private sealed class StubApprovalQueueService : IApprovalQueueService
    {
        private readonly Guid _actorId;

        public StubApprovalQueueService(Guid actorId)
        {
            _actorId = actorId;
        }

        public ApprovalDecisionCommand? LastCommand { get; private set; }

        public OperationResult<ApprovalDecisionReceiptDto> ApproveResult { get; set; } =
            OperationResult<ApprovalDecisionReceiptDto>.Success(
                new ApprovalDecisionReceiptDto(Guid.NewGuid(), "BG-2026-6101", "ApprovalDecision_Approved"));

        public Task<ApprovalQueueSnapshotDto> GetWorkspaceAsync(
            Guid? approverActorId,
            int pageNumber = 1,
            CancellationToken cancellationToken = default)
        {
            var actor = new ApprovalActorSummaryDto(_actorId, "approver.one", "Approver One");

            return Task.FromResult(
                new ApprovalQueueSnapshotDto(
                    actor,
                    [actor],
                    [
                        new ApprovalQueueItemDto(
                            Guid.NewGuid(),
                            "BG-2026-6101",
                            "GuaranteeCategory_Contract",
                            "RequestType_Extend",
                            "RequestChannel_RequestWorkspace",
                            "RequestStatus_InApproval",
                            "Request Owner",
                            DateTimeOffset.UtcNow.AddHours(-2),
                            DateTimeOffset.UtcNow.AddHours(-1),
                            null,
                            "2027-06-30",
                            "Need approval",
                            "WorkflowStage_GuaranteesSupervisor_Title",
                            "Guarantees Supervisor",
                            "Guarantees Supervisor",
                            null,
                            null,
                            null,
                            true,
                            new ApprovalGovernanceStatusDto(false, null, null, null, null, null, null, null),
                            [],
                            [
                                new ApprovalRequestAttachmentDto(
                                    Guid.NewGuid(),
                                    Guid.NewGuid(),
                                    "request-support.pdf",
                                    "GuaranteeDocumentType_SupportingDocument",
                                    DateTimeOffset.UtcNow.AddHours(-2),
                                    "Request Owner",
                                    DateTimeOffset.UtcNow.AddHours(-3),
                                    "Request Owner",
                                    "IntakeCaptureChannel_ManualUpload",
                                    null,
                                    null,
                                    new GuaranteeDocumentFormSnapshotDto(
                                        "supporting-attachment-generic",
                                        "BankProfile_Generic",
                                        "DocumentForm_Attachment_Generic_Title",
                                        "DocumentForm_Attachment_Generic_Summary"))
                            ],
                            [
                                new ApprovalRequestTimelineEntryDto(
                                    Guid.NewGuid(),
                                    DateTimeOffset.UtcNow.AddHours(-2),
                                    "Request Owner",
                                    "Request recorded: Extend via RequestWorkspace.")
                            ])
                    ],
                    new PageInfoDto(pageNumber, 10, 1),
                    true,
                    "ApprovalQueue_ActorScopedNotice"));
        }

        public Task<ApprovalRequestDossierSnapshotDto> GetDossierAsync(
            Guid? approverActorId,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            var actor = new ApprovalActorSummaryDto(_actorId, "approver.one", "Approver One");

            return Task.FromResult(
                new ApprovalRequestDossierSnapshotDto(
                    actor,
                    [actor],
                    null,
                    true,
                    "ApprovalQueue_ActorScopedNotice",
                    "ApprovalDossier_RequestNotAvailable"));
        }

        public Task<OperationResult<ApprovalDecisionReceiptDto>> ApproveAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(ApproveResult);
        }

        public Task<OperationResult<ApprovalDecisionReceiptDto>> ReturnAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(
                OperationResult<ApprovalDecisionReceiptDto>.Success(
                    new ApprovalDecisionReceiptDto(command.RequestId, "BG-2026-6101", "ApprovalDecision_Returned")));
        }

        public Task<OperationResult<ApprovalDecisionReceiptDto>> RejectAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(
                OperationResult<ApprovalDecisionReceiptDto>.Success(
                    new ApprovalDecisionReceiptDto(command.RequestId, "BG-2026-6101", "ApprovalDecision_Rejected")));
        }

        public Task<DocumentContentResult?> GetDocumentContentAsync(Guid requestId, Guid documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentContentResult?>(null);
        }
    }

    private sealed class PassThroughLocalizer : IStringLocalizer<SharedResource>
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

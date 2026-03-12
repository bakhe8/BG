using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Web.Pages.Approvals;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class ApprovalDossierPageTests
{
    [Fact]
    public async Task OnGetAsync_loads_dossier_for_active_actor()
    {
        var actorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var service = new StubApprovalQueueService(actorId);
        var model = new DossierModel(service)
        {
            Actor = actorId,
            PageNumber = 2
        };

        await model.OnGetAsync(requestId, CancellationToken.None);

        Assert.True(model.Snapshot.HasEligibleActor);
        Assert.Equal(actorId, model.Snapshot.ActiveActor!.Id);
        Assert.NotNull(model.Snapshot.Item);
        Assert.Equal(requestId, service.LastDossierRequestId);
        Assert.Equal(actorId, service.LastDossierActorId);
    }

    [Fact]
    public async Task OnGetAsync_uses_locked_actor_context_and_omits_actor_from_queue_route()
    {
        var lockedActorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var service = new StubApprovalQueueService(lockedActorId);
        var model = new DossierModel(service)
        {
            PageNumber = 4
        };
        AttachAuthenticatedUser(model, lockedActorId);

        await model.OnGetAsync(requestId, CancellationToken.None);

        var route = model.BuildQueueRoute();

        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastDossierActorId);
        Assert.Equal("4", route["page"]);
        Assert.False(route.ContainsKey("actor"));
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

        public Guid? LastDossierActorId { get; private set; }

        public Guid? LastDossierRequestId { get; private set; }

        public Task<ApprovalQueueSnapshotDto> GetWorkspaceAsync(
            Guid? approverActorId,
            int pageNumber = 1,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ApprovalRequestDossierSnapshotDto> GetDossierAsync(
            Guid? approverActorId,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            LastDossierActorId = approverActorId;
            LastDossierRequestId = requestId;

            var actor = new ApprovalActorSummaryDto(_actorId, "approver.one", "Approver One");
            var item = new ApprovalQueueItemDto(
                requestId,
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
                [],
                [
                    new ApprovalRequestTimelineEntryDto(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow.AddHours(-2),
                        "Request Owner",
                        "Request recorded: Extend via RequestWorkspace.",
                        null,
                        null,
                        null,
                        null,
                        "DispatchLedgerStep_Printed",
                        "DispatchPrintMode_PdfExport")
                ]);

            return Task.FromResult(
                new ApprovalRequestDossierSnapshotDto(
                    actor,
                    [actor],
                    item,
                    true,
                    "ApprovalQueue_ActorScopedNotice",
                    null));
        }

        public Task<BG.Application.Common.OperationResult<ApprovalDecisionReceiptDto>> ApproveAsync(
            ApprovalDecisionCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BG.Application.Common.OperationResult<ApprovalDecisionReceiptDto>> ReturnAsync(
            ApprovalDecisionCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BG.Application.Common.OperationResult<ApprovalDecisionReceiptDto>> RejectAsync(
            ApprovalDecisionCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

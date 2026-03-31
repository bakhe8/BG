using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.Web.Pages.Approvals;

[Authorize(Policy = PermissionPolicyNames.ApprovalsQueue)]
public sealed class DossierModel : PageModel
{
    private readonly IApprovalQueueService _approvalQueueService;

    public DossierModel(IApprovalQueueService approvalQueueService)
    {
        _approvalQueueService = approvalQueueService;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    public Guid RequestId { get; private set; }

    public ApprovalRequestDossierSnapshotDto Snapshot { get; private set; } = default!;

    public bool IsActorContextLocked { get; private set; }

    public async Task OnGetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        RequestId = requestId;
        await LoadAsync(requestId, Actor, cancellationToken);
    }

    public IDictionary<string, string> BuildQueueRoute()
    {
        var routeValues = new Dictionary<string, string>
        {
            ["page"] = (PageNumber ?? 1).ToString(),
            ["request"] = RequestId.ToString()
        };

        if (!IsActorContextLocked && Snapshot.ActiveActor is not null)
        {
            routeValues["actor"] = Snapshot.ActiveActor.Id.ToString();
        }

        return routeValues;
    }

    private async Task LoadAsync(Guid requestId, Guid? actorId, CancellationToken cancellationToken)
    {
        actorId = ResolveActor(actorId);
        Snapshot = await _approvalQueueService.GetDossierAsync(actorId, requestId, cancellationToken);
    }

    private Guid? ResolveActor(Guid? actorId)
    {
        var lockedActorId = WorkspaceActorContext.TryGetLockedActorUserId(HttpContext);
        IsActorContextLocked = lockedActorId.HasValue;
        return lockedActorId ?? actorId;
    }
}

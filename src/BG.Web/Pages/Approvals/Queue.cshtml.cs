using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Approvals;

[Authorize(Policy = PermissionPolicyNames.ApprovalsQueue)]
public sealed class QueueModel : PageModel
{
    private readonly IApprovalQueueService _approvalQueueService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public QueueModel(
        IApprovalQueueService approvalQueueService,
        IStringLocalizer<SharedResource> localizer)
    {
        _approvalQueueService = approvalQueueService;
        _localizer = localizer;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    [FromQuery(Name = "request")]
    public Guid? SelectedRequestId { get; set; }

    public ApprovalQueueSnapshotDto Snapshot { get; private set; } = default!;

    public ApprovalQueueItemDto? ActiveItem { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(Actor, PageNumber, SelectedRequestId, cancellationToken);
    }

    public async Task<IActionResult> OnPostApproveAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        Guid? request,
        CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            actorId,
            requestId,
            note,
            page,
            request,
            (command, token) => _approvalQueueService.ApproveAsync(command, token),
            cancellationToken);
    }

    public async Task<IActionResult> OnPostReturnAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        Guid? request,
        CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            actorId,
            requestId,
            note,
            page,
            request,
            (command, token) => _approvalQueueService.ReturnAsync(command, token),
            cancellationToken);
    }

    public async Task<IActionResult> OnPostRejectAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        Guid? request,
        CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            actorId,
            requestId,
            note,
            page,
            request,
            (command, token) => _approvalQueueService.RejectAsync(command, token),
            cancellationToken);
    }

    private async Task<IActionResult> ApplyAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        Guid? selectedRequestId,
        Func<ApprovalDecisionCommand, CancellationToken, Task<BG.Application.Common.OperationResult<ApprovalDecisionReceiptDto>>> action,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, selectedRequestId ?? requestId, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["ApprovalQueue_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await action(new ApprovalDecisionCommand(effectiveActorId, requestId, note), cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer[
            "ApprovalQueue_DecisionSuccess",
            _localizer[result.Value!.OutcomeResourceKey],
            result.Value.GuaranteeNumber];

        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber, requestId);
    }

    public IDictionary<string, string> BuildPageRoute(int pageNumber)
    {
        var routeValues = new Dictionary<string, string>
        {
            ["page"] = pageNumber.ToString()
        };

        if (!IsActorContextLocked && Snapshot.ActiveActor is not null)
        {
            routeValues["actor"] = Snapshot.ActiveActor.Id.ToString();
        }

        if (ActiveItem is not null)
        {
            routeValues["request"] = ActiveItem.RequestId.ToString();
        }

        return routeValues;
    }

    public IDictionary<string, string> BuildSelectionRoute(Guid requestId, int? pageNumber = null)
    {
        var routeValues = BuildPageRoute(pageNumber ?? Snapshot.ItemsPage.PageNumber);
        routeValues["request"] = requestId.ToString();
        return routeValues;
    }

    public IDictionary<string, string> BuildDossierRoute(Guid requestId)
    {
        var routeValues = new Dictionary<string, string>
        {
            ["requestId"] = requestId.ToString(),
            ["page"] = Snapshot.ItemsPage.PageNumber.ToString()
        };

        if (!IsActorContextLocked && Snapshot.ActiveActor is not null)
        {
            routeValues["actor"] = Snapshot.ActiveActor.Id.ToString();
        }

        return routeValues;
    }

    public string ResolveStageTitle(ApprovalQueueItemDto item)
    {
        return !string.IsNullOrWhiteSpace(item.CurrentStageTitle)
            ? item.CurrentStageTitle
            : string.IsNullOrWhiteSpace(item.CurrentStageTitleResourceKey)
                ? "-"
                : _localizer[item.CurrentStageTitleResourceKey].Value;
    }

    public string ResolveConflictingStageTitle(ApprovalQueueItemDto item)
    {
        return !string.IsNullOrWhiteSpace(item.Governance.ConflictingStageTitle)
            ? item.Governance.ConflictingStageTitle
            : string.IsNullOrWhiteSpace(item.Governance.ConflictingStageTitleResourceKey)
                ? item.Governance.ConflictingStageRoleName ?? "-"
                : _localizer[item.Governance.ConflictingStageTitleResourceKey].Value;
    }

    public string ResolveRequestedChange(ApprovalQueueItemDto item)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.RequestedAmount))
        {
            parts.Add(item.RequestedAmount);
        }

        if (!string.IsNullOrWhiteSpace(item.RequestedExpiryDate))
        {
            parts.Add(item.RequestedExpiryDate);
        }

        return parts.Count == 0 ? "-" : string.Join(" · ", parts);
    }

    private async Task LoadAsync(Guid? actorId, int? pageNumber, Guid? selectedRequestId, CancellationToken cancellationToken)
    {
        actorId = ResolveActor(actorId);
        Snapshot = await _approvalQueueService.GetWorkspaceAsync(actorId, pageNumber ?? 1, cancellationToken);
        ActiveItem = ResolveActiveItem(selectedRequestId);
        SelectedRequestId = ActiveItem?.RequestId;
    }

    private ApprovalQueueItemDto? ResolveActiveItem(Guid? selectedRequestId)
    {
        if (Snapshot.Items.Count == 0)
        {
            return null;
        }

        if (selectedRequestId.HasValue)
        {
            var selectedItem = Snapshot.Items.FirstOrDefault(item => item.RequestId == selectedRequestId.Value);
            if (selectedItem is not null)
            {
                return selectedItem;
            }
        }

        return Snapshot.Items[0];
    }

    private Guid? ResolveActor(Guid? actorId)
    {
        var lockedActorId = WorkspaceActorContext.TryGetLockedActorUserId(HttpContext);
        IsActorContextLocked = lockedActorId.HasValue;
        return lockedActorId ?? actorId;
    }

    private RedirectToPageResult RedirectToSelf(Guid actorId, int pageNumber, Guid? selectedRequestId = null)
    {
        return IsActorContextLocked
            ? RedirectToPage("/Approvals/Queue", new { page = pageNumber, request = selectedRequestId })
            : RedirectToPage("/Approvals/Queue", new { actor = actorId, page = pageNumber, request = selectedRequestId });
    }
}

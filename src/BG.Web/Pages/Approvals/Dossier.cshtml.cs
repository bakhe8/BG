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
public sealed class DossierModel : PageModel
{
    private readonly IApprovalQueueService _approvalQueueService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAuthorizationService _authorizationService;

    public DossierModel(
        IApprovalQueueService approvalQueueService,
        IStringLocalizer<SharedResource> localizer,
        IAuthorizationService authorizationService)
    {
        _approvalQueueService = approvalQueueService;
        _localizer = localizer;
        _authorizationService = authorizationService;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    public Guid RequestId { get; private set; }

    public ApprovalRequestDossierSnapshotDto Snapshot { get; private set; } = default!;

    public bool IsActorContextLocked { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        RequestId = requestId;
        await LoadAsync(requestId, Actor, cancellationToken);
    }

    public async Task<IActionResult> OnGetPreviewAttachmentAsync(Guid requestId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await _approvalQueueService.GetDocumentContentAsync(requestId, attachmentId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return new FileStreamResult(result.ContentStream, result.ContentType)
        {
            FileDownloadName = result.FileName,
            EnableRangeProcessing = true
        };
    }

    public async Task<IActionResult> OnPostApproveAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            actorId,
            requestId,
            note,
            page,
            (command, token) => _approvalQueueService.ApproveAsync(command, token),
            cancellationToken);
    }

    public async Task<IActionResult> OnPostReturnAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            actorId,
            requestId,
            note,
            page,
            (command, token) => _approvalQueueService.ReturnAsync(command, token),
            cancellationToken);
    }

    public async Task<IActionResult> OnPostRejectAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? page,
        CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            actorId,
            requestId,
            note,
            page,
            (command, token) => _approvalQueueService.RejectAsync(command, token),
            cancellationToken);
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
        RequestId = requestId;
        actorId = ResolveActor(actorId);
        Snapshot = await _approvalQueueService.GetDossierAsync(actorId, requestId, cancellationToken);
    }

    private async Task<IActionResult> ApplyAsync(
        Guid actorId,
        Guid requestId,
        string? note,
        int? pageNumber,
        Func<ApprovalDecisionCommand, CancellationToken, Task<BG.Application.Common.OperationResult<ApprovalDecisionReceiptDto>>> action,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, PermissionPolicyNames.ApprovalsSign);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        PageNumber = pageNumber;
        await LoadAsync(requestId, actorId, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["ApprovalQueue_NoEligibleActor"]);
            return Page();
        }

        if (Snapshot.Item is null)
        {
            ModelState.AddModelError(
                string.Empty,
                _localizer[Snapshot.UnavailableResourceKey ?? "ApprovalDossier_RequestNotAvailable"]);
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

        return RedirectToQueue(effectiveActorId, PageNumber ?? 1, requestId);
    }

    private Guid? ResolveActor(Guid? actorId)
    {
        var lockedActorId = WorkspaceActorContext.TryGetLockedActorUserId(HttpContext);
        IsActorContextLocked = lockedActorId.HasValue;
        return lockedActorId ?? actorId;
    }

    private RedirectToPageResult RedirectToQueue(Guid actorId, int pageNumber, Guid? selectedRequestId = null)
    {
        return IsActorContextLocked
            ? RedirectToPage("/Approvals/Queue", new { page = pageNumber, request = selectedRequestId })
            : RedirectToPage("/Approvals/Queue", new { actor = actorId, page = pageNumber, request = selectedRequestId });
    }
}

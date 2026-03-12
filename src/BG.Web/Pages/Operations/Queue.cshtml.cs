using BG.Application.Contracts.Services;
using BG.Application.Operations;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Operations;

[Authorize(Policy = PermissionPolicyNames.OperationsQueue)]
public sealed class QueueModel : PageModel
{
    private readonly IOperationsReviewQueueService _operationsReviewQueueService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public QueueModel(
        IOperationsReviewQueueService operationsReviewQueueService,
        IStringLocalizer<SharedResource> localizer)
    {
        _operationsReviewQueueService = operationsReviewQueueService;
        _localizer = localizer;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    public OperationsReviewQueueSnapshotDto Snapshot { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(Actor, PageNumber, cancellationToken);
    }

    public async Task<IActionResult> OnPostApplyAsync(
        Guid actorId,
        Guid reviewItemId,
        Guid requestId,
        string? confirmedExpiryDate,
        string? confirmedAmount,
        string? replacementGuaranteeNumber,
        string? note,
        int? page,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["OperationsQueue_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _operationsReviewQueueService.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                effectiveActorId,
                reviewItemId,
                requestId,
                confirmedExpiryDate,
                confirmedAmount,
                replacementGuaranteeNumber,
                note),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer["OperationsQueue_ApplySuccess", result.Value!.GuaranteeNumber];
        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber);
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

        return routeValues;
    }

    private async Task LoadAsync(Guid? actorId, int? pageNumber, CancellationToken cancellationToken)
    {
        actorId = ResolveActor(actorId);
        Snapshot = await _operationsReviewQueueService.GetSnapshotAsync(actorId, pageNumber ?? 1, cancellationToken);
    }

    private Guid? ResolveActor(Guid? actorId)
    {
        var lockedActorId = WorkspaceActorContext.TryGetLockedActorUserId(HttpContext);
        IsActorContextLocked = lockedActorId.HasValue;
        return lockedActorId ?? actorId;
    }

    private RedirectToPageResult RedirectToSelf(Guid actorId, int pageNumber)
    {
        return IsActorContextLocked
            ? RedirectToPage("/Operations/Queue", new { page = pageNumber })
            : RedirectToPage("/Operations/Queue", new { actor = actorId, page = pageNumber });
    }
}

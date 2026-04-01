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

    [FromQuery(Name = "item")]
    public Guid? SelectedItemId { get; set; }

    public OperationsReviewQueueSnapshotDto Snapshot { get; private set; } = default!;

    public OperationsReviewItemDto? ActiveItem { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(Actor, PageNumber, SelectedItemId, cancellationToken);
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
        Guid? item,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, item ?? reviewItemId, cancellationToken);

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
        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber, reviewItemId);
    }

    public async Task<IActionResult> OnPostReopenAppliedAsync(
        Guid actorId,
        Guid reviewItemId,
        string? correctionNote,
        int? page,
        Guid? item,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, item ?? reviewItemId, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["OperationsQueue_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _operationsReviewQueueService.ReopenAppliedBankResponseAsync(
            new ReopenAppliedBankResponseCommand(
                effectiveActorId,
                reviewItemId,
                correctionNote),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer["OperationsQueue_ReopenSuccess", result.Value!.GuaranteeNumber];
        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber, reviewItemId);
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

    public IDictionary<string, string> BuildSelectionRoute(Guid itemId, int? pageNumber = null)
    {
        var routeValues = BuildPageRoute(pageNumber ?? Snapshot.ItemsPage.PageNumber);
        routeValues["item"] = itemId.ToString();
        return routeValues;
    }

    public string ResolveSuggestionOptionLabel(OperationsReviewMatchSuggestionDto suggestion)
    {
        var label = $"{_localizer[suggestion.RequestTypeResourceKey].Value} - {_localizer[suggestion.StatusResourceKey].Value} ({suggestion.Score})";
        return suggestion.IsSelectionBlocked
            ? $"{label} - {_localizer["OperationsQueue_BlockedSuggestionLabel"].Value}"
            : label;
    }

    public IReadOnlyList<OperationsReviewMatchSuggestionDto> GetSelectionSuggestions(OperationsReviewItemDto item)
    {
        return item.MatchSuggestions
            .OrderBy(suggestion => suggestion.IsSelectionBlocked ? 1 : 0)
            .ThenByDescending(suggestion => suggestion.Score)
            .ThenByDescending(suggestion => suggestion.SubmittedToBankAtUtc)
            .ToArray();
    }

    public bool IsApplyBlocked(OperationsReviewItemDto item)
    {
        return item.SupportsRequestMatching &&
               item.MatchSuggestions.Count > 0 &&
               item.MatchSuggestions.All(suggestion => suggestion.IsSelectionBlocked);
    }

    public string? ResolveApplyBlockedReasonResourceKey(OperationsReviewItemDto item)
    {
        if (!IsApplyBlocked(item))
        {
            return null;
        }

        return item.MatchSuggestions
            .Select(suggestion => suggestion.BlockingReasonResourceKey)
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));
    }

    private async Task LoadAsync(Guid? actorId, int? pageNumber, Guid? selectedItemId, CancellationToken cancellationToken)
    {
        actorId = ResolveActor(actorId);
        Snapshot = await _operationsReviewQueueService.GetSnapshotAsync(actorId, pageNumber ?? 1, cancellationToken);
        ActiveItem = await ResolveActiveItemAsync(selectedItemId, cancellationToken);
        SelectedItemId = ActiveItem?.Id;
    }

    private async Task<OperationsReviewItemDto?> ResolveActiveItemAsync(Guid? selectedItemId, CancellationToken cancellationToken)
    {
        if (Snapshot.Items.Count == 0)
        {
            return selectedItemId.HasValue
                ? await _operationsReviewQueueService.GetCompletedItemAsync(selectedItemId.Value, cancellationToken)
                : null;
        }

        if (selectedItemId.HasValue)
        {
            var selectedItem = Snapshot.Items.FirstOrDefault(item => item.Id == selectedItemId.Value);
            if (selectedItem is not null)
            {
                return selectedItem;
            }

            var completedItem = await _operationsReviewQueueService.GetCompletedItemAsync(selectedItemId.Value, cancellationToken);
            if (completedItem is not null)
            {
                return completedItem;
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

    private RedirectToPageResult RedirectToSelf(Guid actorId, int pageNumber, Guid? selectedItemId = null)
    {
        return IsActorContextLocked
            ? RedirectToPage("/Operations/Queue", new { page = pageNumber, item = selectedItemId })
            : RedirectToPage("/Operations/Queue", new { actor = actorId, page = pageNumber, item = selectedItemId });
    }
}

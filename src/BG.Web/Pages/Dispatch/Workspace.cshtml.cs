using BG.Application.Contracts.Services;
using BG.Application.Models.Dispatch;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Dispatch;

[Authorize(Policy = PermissionPolicyNames.DispatchWorkspace)]
public sealed class WorkspaceModel : PageModel
{
    private readonly IDispatchWorkspaceService _dispatchWorkspaceService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkspaceModel(
        IDispatchWorkspaceService dispatchWorkspaceService,
        IStringLocalizer<SharedResource> localizer)
    {
        _dispatchWorkspaceService = dispatchWorkspaceService;
        _localizer = localizer;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    public DispatchWorkspaceSnapshotDto Snapshot { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public IReadOnlyList<DispatchPrintModeOptionViewModel> PrintModeOptions { get; } =
        GuaranteeResourceCatalog.GetSupportedDispatchPrintModes()
            .Select(mode => new DispatchPrintModeOptionViewModel(mode, GuaranteeResourceCatalog.GetDispatchPrintModeResourceKey(mode)))
            .ToArray();

    public IReadOnlyList<DispatchChannelOptionViewModel> DispatchChannelOptions { get; } =
        GuaranteeResourceCatalog.GetSupportedDispatchChannels()
            .Select(channel => new DispatchChannelOptionViewModel(channel, GuaranteeResourceCatalog.GetDispatchChannelResourceKey(channel)))
            .ToArray();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(Actor, PageNumber, cancellationToken);
    }

    public async Task<IActionResult> OnPostPrintAsync(
        Guid actorId,
        Guid requestId,
        string? referenceNumber,
        string? letterDate,
        GuaranteeOutgoingLetterPrintMode? printMode,
        int? page,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["DispatchWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _dispatchWorkspaceService.PrintDispatchLetterAsync(
            new PrintDispatchLetterCommand(effectiveActorId, requestId, referenceNumber, letterDate, printMode),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer[
            "DispatchWorkspace_PrintSuccess",
            result.Value!.ReferenceNumber,
            result.Value.GuaranteeNumber,
            result.Value.PrintCount];

        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber);
    }

    public async Task<IActionResult> OnPostDispatchAsync(
        Guid actorId,
        Guid requestId,
        string? referenceNumber,
        string? letterDate,
        GuaranteeDispatchChannel? dispatchChannel,
        string? dispatchReference,
        string? note,
        int? page,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["DispatchWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _dispatchWorkspaceService.RecordDispatchAsync(
            new RecordDispatchCommand(effectiveActorId, requestId, referenceNumber, letterDate, dispatchChannel, dispatchReference, note),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer[
            "DispatchWorkspace_DispatchSuccess",
            result.Value!.ReferenceNumber,
            result.Value.GuaranteeNumber];

        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber);
    }

    public async Task<IActionResult> OnPostConfirmDeliveryAsync(
        Guid actorId,
        Guid requestId,
        Guid correspondenceId,
        string? deliveryReference,
        string? deliveryNote,
        int? page,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["DispatchWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _dispatchWorkspaceService.ConfirmDeliveryAsync(
            new ConfirmDispatchDeliveryCommand(
                effectiveActorId,
                requestId,
                correspondenceId,
                deliveryReference,
                deliveryNote),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer[
            "DispatchWorkspace_DeliverySuccess",
            result.Value!.ReferenceNumber,
            result.Value.GuaranteeNumber];

        return RedirectToSelf(effectiveActorId, Snapshot.ItemsPage.PageNumber);
    }

    public async Task<IActionResult> OnPostReopenDispatchAsync(
        Guid actorId,
        Guid requestId,
        Guid correspondenceId,
        string? correctionNote,
        int? page,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["DispatchWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _dispatchWorkspaceService.ReopenDispatchAsync(
            new ReopenDispatchCommand(
                effectiveActorId,
                requestId,
                correspondenceId,
                correctionNote),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer[
            "DispatchWorkspace_ReopenSuccess",
            result.Value!.ReferenceNumber,
            result.Value.GuaranteeNumber];

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
        Snapshot = await _dispatchWorkspaceService.GetWorkspaceAsync(actorId, pageNumber ?? 1, cancellationToken);
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
            ? RedirectToPage("/Dispatch/Workspace", new { page = pageNumber })
            : RedirectToPage("/Dispatch/Workspace", new { actor = actorId, page = pageNumber });
    }

    public IReadOnlyList<DispatchChannelOptionViewModel> GetAvailableDispatchChannelOptions()
    {
        if (Snapshot.ActiveActor?.CanEmail == true && Snapshot.ActiveActor.CanRecord)
        {
            return DispatchChannelOptions;
        }

        if (Snapshot.ActiveActor?.CanEmail == true)
        {
            return DispatchChannelOptions
                .Where(option => option.Channel == GuaranteeDispatchChannel.OfficialEmail)
                .ToArray();
        }

        return DispatchChannelOptions
            .Where(option => option.Channel != GuaranteeDispatchChannel.OfficialEmail)
            .ToArray();
    }

    public sealed record DispatchPrintModeOptionViewModel(GuaranteeOutgoingLetterPrintMode Mode, string ResourceKey);

    public sealed record DispatchChannelOptionViewModel(GuaranteeDispatchChannel Channel, string ResourceKey);
}

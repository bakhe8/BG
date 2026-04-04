using BG.Application.Contracts.Services;
using BG.Application.Dispatch;
using BG.Application.Models.Dispatch;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Dispatch;

[Authorize(Policy = PermissionPolicyNames.DispatchWorkspace)]
public sealed class LetterModel : PageModel
{
    private readonly IDispatchWorkspaceService _dispatchWorkspaceService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LetterModel(
        IDispatchWorkspaceService dispatchWorkspaceService,
        IStringLocalizer<SharedResource> localizer)
    {
        _dispatchWorkspaceService = dispatchWorkspaceService;
        _localizer = localizer;
    }

    [FromQuery(Name = "actorId")]
    public Guid? ActorId { get; set; }

    [FromQuery(Name = "requestId")]
    public Guid RequestId { get; set; }

    [FromQuery(Name = "referenceNumber")]
    public string? ReferenceNumber { get; set; }

    [FromQuery(Name = "letterDate")]
    public string? LetterDate { get; set; }

    [FromQuery(Name = "autoPrint")]
    public bool AutoPrint { get; set; }

    public DispatchLetterPreviewDto? Letter { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsActorContextLocked { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var resolvedActorId = ResolveActor(ActorId);
        if (!resolvedActorId.HasValue)
        {
            ErrorMessage = _localizer[DispatchErrorCodes.DispatcherContextRequired];
            return Page();
        }

        if (RequestId == Guid.Empty)
        {
            ErrorMessage = _localizer[DispatchErrorCodes.RequestNotFound];
            return Page();
        }

        var result = await _dispatchWorkspaceService.GetLetterPreviewAsync(
            resolvedActorId.Value,
            RequestId,
            ReferenceNumber,
            LetterDate,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            ErrorMessage = _localizer[result.ErrorCode ?? DispatchErrorCodes.RequestNotFound];
            return Page();
        }

        Letter = result.Value;
        return Page();
    }

    public async Task<IActionResult> OnGetPdfAsync(
        Guid requestId,
        string? referenceNumber,
        string? letterDate,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var resolvedActorId = ResolveActor(actorId ?? ActorId);
        if (!resolvedActorId.HasValue)
            return RedirectToPage("/Dispatch/Workspace", new { shellMessage = DispatchErrorCodes.DispatcherContextRequired });

        if (requestId == Guid.Empty)
            return RedirectToPage("/Dispatch/Workspace", new { shellMessage = DispatchErrorCodes.RequestNotFound });

        if (string.IsNullOrWhiteSpace(referenceNumber) || string.IsNullOrWhiteSpace(letterDate))
            return RedirectToPage("/Dispatch/Letter", new { requestId, actorId = resolvedActorId, referenceNumber, letterDate });

        if (!DateOnly.TryParse(letterDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedLetterDate) &&
            !DateOnly.TryParse(letterDate, out parsedLetterDate))
            return RedirectToPage("/Dispatch/Letter", new { requestId, actorId = resolvedActorId, referenceNumber, letterDate });

        var result = await _dispatchWorkspaceService.GetLetterPdfAsync(
            resolvedActorId.Value,
            requestId,
            referenceNumber,
            parsedLetterDate,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
            return RedirectToPage("/Dispatch/Letter", new { requestId, actorId = resolvedActorId, referenceNumber, letterDate });

        return File(result.Value.Content, "application/pdf", result.Value.FileName);
    }

    public object BuildWorkspaceRoute()
    {
        return IsActorContextLocked
            ? new { request = RequestId }
            : new { actor = ActorId, request = RequestId };
    }

    private Guid? ResolveActor(Guid? actorId)
    {
        var lockedActorId = WorkspaceActorContext.TryGetLockedActorUserId(HttpContext);
        IsActorContextLocked = lockedActorId.HasValue;
        ActorId = lockedActorId ?? actorId;
        return ActorId;
    }
}

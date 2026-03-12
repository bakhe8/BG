using BG.Application.Contracts.Services;
using BG.Application.Models.Requests;
using BG.Application.Operations;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Requests;

[Authorize(Policy = PermissionPolicyNames.RequestsWorkspace)]
public sealed class WorkspaceModel : PageModel
{
    private readonly IRequestWorkspaceService _requestWorkspaceService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkspaceModel(
        IRequestWorkspaceService requestWorkspaceService,
        IStringLocalizer<SharedResource> localizer)
    {
        _requestWorkspaceService = requestWorkspaceService;
        _localizer = localizer;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    [BindProperty]
    public CreateRequestInput Input { get; set; } = new();

    public RequestWorkspaceSnapshotDto Snapshot { get; private set; } = default!;

    public RequestWorkflowTemplateDto? SelectedWorkflowTemplate { get; private set; }

    public IReadOnlyList<RequestTypeOptionViewModel> RequestTypeOptions { get; private set; } = Array.Empty<RequestTypeOptionViewModel>();

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public bool RequiresAmount => GuaranteeResourceCatalog.RequiresRequestedAmount(Input.RequestType);

    public bool RequiresExpiryDate => GuaranteeResourceCatalog.RequiresRequestedExpiryDate(Input.RequestType);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(Actor, PageNumber, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(int? page, CancellationToken cancellationToken)
    {
        await LoadAsync(Input.RequestedByUserId == Guid.Empty ? Actor : Input.RequestedByUserId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["RequestsWorkspace_NoEligibleActor"]);
            return Page();
        }

        Input.RequestedByUserId = Snapshot.ActiveActor.Id;

        var result = await _requestWorkspaceService.CreateRequestAsync(
            new CreateGuaranteeRequestCommand(
                Input.RequestedByUserId,
                Input.GuaranteeNumber,
                Input.RequestType,
                Input.RequestedAmount,
                Input.RequestedExpiryDate,
                Input.Notes),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer[
            "RequestsWorkspace_CreateSuccess",
            _localizer[result.Value!.RequestTypeResourceKey],
            result.Value.GuaranteeNumber];

        return RedirectToSelf(Input.RequestedByUserId, pageNumber: 1);
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid requestId, Guid actorId, int? page, CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["RequestsWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;

        var result = await _requestWorkspaceService.SubmitRequestForApprovalAsync(effectiveActorId, requestId, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        var currentStageLabel = !string.IsNullOrWhiteSpace(result.Value!.CurrentStageTitle)
            ? result.Value.CurrentStageTitle
            : result.Value.CurrentStageTitleResourceKey is null
                ? "-"
                : _localizer[result.Value.CurrentStageTitleResourceKey].Value;

        StatusMessage = _localizer[
            "RequestsWorkspace_SubmitSuccess",
            currentStageLabel,
            result.Value.CurrentStageRoleName ?? "-"];

        return RedirectToSelf(effectiveActorId, Snapshot.OwnedRequestsPage.PageNumber);
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
        Snapshot = await _requestWorkspaceService.GetWorkspaceAsync(actorId, pageNumber ?? 1, cancellationToken);
        RequestTypeOptions = BuildRequestTypeOptions();

        if (Snapshot.ActiveActor is not null)
        {
            Input.RequestedByUserId = Snapshot.ActiveActor.Id;
        }

        SelectedWorkflowTemplate = await _requestWorkspaceService.GetWorkflowTemplateAsync(
            Input.GuaranteeNumber,
            Input.RequestType,
            cancellationToken);
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
            ? RedirectToPage("/Requests/Workspace", new { page = pageNumber })
            : RedirectToPage("/Requests/Workspace", new { actor = actorId, page = pageNumber });
    }

    private IReadOnlyList<RequestTypeOptionViewModel> BuildRequestTypeOptions()
    {
        return GuaranteeResourceCatalog.GetSupportedRequestTypes()
            .Select(requestType => new RequestTypeOptionViewModel(requestType, GuaranteeResourceCatalog.GetRequestTypeResourceKey(requestType)))
            .ToArray();
    }

    public sealed class CreateRequestInput
    {
        public Guid RequestedByUserId { get; set; }

        public string GuaranteeNumber { get; set; } = string.Empty;

        public GuaranteeRequestType RequestType { get; set; } = GuaranteeRequestType.Extend;

        public string? RequestedAmount { get; set; }

        public string? RequestedExpiryDate { get; set; }

        public string? Notes { get; set; }
    }

    public sealed record RequestTypeOptionViewModel(GuaranteeRequestType Type, string ResourceKey);
}

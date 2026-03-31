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

    [FromQuery(Name = "request")]
    public Guid? SelectedRequestId { get; set; }

    [BindProperty]
    public CreateRequestInput Input { get; set; } = new();

    public RequestWorkspaceSnapshotDto Snapshot { get; private set; } = default!;

    public RequestWorkflowTemplateDto? SelectedWorkflowTemplate { get; private set; }

    public RequestSummaryDto? ActiveRequest { get; private set; }

    public IReadOnlyList<RequestTypeOptionViewModel> RequestTypeOptions { get; private set; } = Array.Empty<RequestTypeOptionViewModel>();

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public bool RequiresAmount => GuaranteeResourceCatalog.RequiresRequestedAmount(Input.RequestType);

    public bool RequiresExpiryDate => GuaranteeResourceCatalog.RequiresRequestedExpiryDate(Input.RequestType);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(Actor, PageNumber, SelectedRequestId, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(int? page, Guid? request, CancellationToken cancellationToken)
    {
        await LoadAsync(Input.RequestedByUserId == Guid.Empty ? Actor : Input.RequestedByUserId, page, request, cancellationToken);

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

        return RedirectToSelf(Input.RequestedByUserId, pageNumber: 1, selectedRequestId: result.Value.RequestId);
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid requestId, Guid actorId, int? page, Guid? request, CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, request ?? requestId, cancellationToken);

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

        return RedirectToSelf(effectiveActorId, Snapshot.OwnedRequestsPage.PageNumber, requestId);
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid requestId,
        Guid actorId,
        string? requestedAmount,
        string? requestedExpiryDate,
        string? notes,
        int? page,
        Guid? request,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, request ?? requestId, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["RequestsWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;
        var result = await _requestWorkspaceService.UpdateRequestAsync(
            new UpdateGuaranteeRequestCommand(
                effectiveActorId,
                requestId,
                requestedAmount,
                requestedExpiryDate,
                notes),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer["RequestsWorkspace_UpdateSuccess", result.Value!.GuaranteeNumber];
        return RedirectToSelf(effectiveActorId, Snapshot.OwnedRequestsPage.PageNumber, requestId);
    }

    public async Task<IActionResult> OnPostCancelAsync(
        Guid requestId,
        Guid actorId,
        int? page,
        Guid? request,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, request ?? requestId, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["RequestsWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;
        var result = await _requestWorkspaceService.CancelRequestAsync(effectiveActorId, requestId, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer["RequestsWorkspace_CancelSuccess", result.Value!.GuaranteeNumber];
        return RedirectToSelf(effectiveActorId, Snapshot.OwnedRequestsPage.PageNumber, requestId);
    }

    public async Task<IActionResult> OnPostWithdrawAsync(
        Guid requestId,
        Guid actorId,
        int? page,
        Guid? request,
        CancellationToken cancellationToken)
    {
        await LoadAsync(actorId, page, request ?? requestId, cancellationToken);

        if (!Snapshot.HasEligibleActor || Snapshot.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["RequestsWorkspace_NoEligibleActor"]);
            return Page();
        }

        var effectiveActorId = Snapshot.ActiveActor.Id;
        var result = await _requestWorkspaceService.WithdrawRequestAsync(effectiveActorId, requestId, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            return Page();
        }

        StatusMessage = _localizer["RequestsWorkspace_WithdrawSuccess", result.Value!.GuaranteeNumber];
        return RedirectToSelf(effectiveActorId, Snapshot.OwnedRequestsPage.PageNumber, requestId);
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

    public IDictionary<string, string> BuildSelectionRoute(Guid requestId, int? pageNumber = null)
    {
        var routeValues = BuildPageRoute(pageNumber ?? Snapshot.OwnedRequestsPage.PageNumber);
        routeValues["request"] = requestId.ToString();
        return routeValues;
    }

    public string ResolveStageTitle(RequestSummaryDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.CurrentStageTitle))
        {
            return request.CurrentStageTitle;
        }

        return string.IsNullOrWhiteSpace(request.CurrentStageTitleResourceKey)
            ? "-"
            : _localizer[request.CurrentStageTitleResourceKey].Value;
    }

    public string ResolveStateSummaryResourceKey(string statusResourceKey)
    {
        return statusResourceKey switch
        {
            "RequestStatus_Draft" => "RequestsWorkspace_State_Draft",
            "RequestStatus_InApproval" => "RequestsWorkspace_State_InApproval",
            "RequestStatus_Returned" => "RequestsWorkspace_State_Returned",
            "RequestStatus_ApprovedForDispatch" => "RequestsWorkspace_State_ApprovedForDispatch",
            "RequestStatus_SubmittedToBank" => "RequestsWorkspace_State_AwaitingBankResponse",
            "RequestStatus_AwaitingBankResponse" => "RequestsWorkspace_State_AwaitingBankResponse",
            "RequestStatus_Completed" => "RequestsWorkspace_State_Completed",
            "RequestStatus_Rejected" => "RequestsWorkspace_State_Rejected",
            "RequestStatus_Cancelled" => "RequestsWorkspace_State_Cancelled",
            _ => "RequestsWorkspace_State_Draft"
        };
    }

    public string ResolveNextActionResourceKey(RequestSummaryDto request)
    {
        if (request.CanSubmitForApproval)
        {
            return "RequestsWorkspace_NextActionSubmit";
        }

        if (request.CanRevise)
        {
            return "RequestsWorkspace_NextActionRevise";
        }

        if (request.CanWithdraw)
        {
            return "RequestsWorkspace_NextActionWithdraw";
        }

        return request.StatusResourceKey switch
        {
            "RequestStatus_ApprovedForDispatch" or "RequestStatus_SubmittedToBank" or "RequestStatus_AwaitingBankResponse"
                => "RequestsWorkspace_NextActionAwaitExternal",
            "RequestStatus_Completed" => "RequestsWorkspace_NextActionCompleted",
            "RequestStatus_Rejected" or "RequestStatus_Cancelled" => "RequestsWorkspace_NextActionClosed",
            _ => "RequestsWorkspace_NextActionMonitor"
        };
    }

    public string ResolveRequestedChange(RequestSummaryDto request)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.RequestedAmount))
        {
            parts.Add(request.RequestedAmount);
        }

        if (!string.IsNullOrWhiteSpace(request.RequestedExpiryDate))
        {
            parts.Add(request.RequestedExpiryDate);
        }

        return parts.Count == 0 ? "-" : string.Join(" · ", parts);
    }

    public bool HasOwnerAction(RequestSummaryDto request)
    {
        return request.CanSubmitForApproval || request.CanRevise || request.CanCancel || request.CanWithdraw;
    }

    public string? ResolveNoOwnerActionReasonResourceKey(RequestSummaryDto request)
    {
        if (HasOwnerAction(request))
        {
            return null;
        }

        return request.StatusResourceKey switch
        {
            "RequestStatus_InApproval" => "RequestsWorkspace_NoOwnerAction_InApproval",
            "RequestStatus_ApprovedForDispatch" => "RequestsWorkspace_NoOwnerAction_ApprovedForDispatch",
            "RequestStatus_SubmittedToBank" or "RequestStatus_AwaitingBankResponse" => "RequestsWorkspace_NoOwnerAction_AwaitingBankResponse",
            "RequestStatus_Completed" => "RequestsWorkspace_NoOwnerAction_Completed",
            "RequestStatus_Rejected" => "RequestsWorkspace_NoOwnerAction_Rejected",
            "RequestStatus_Cancelled" => "RequestsWorkspace_NoOwnerAction_Cancelled",
            _ => "RequestsWorkspace_NextActionMonitor"
        };
    }

    private async Task LoadAsync(Guid? actorId, int? pageNumber, Guid? selectedRequestId, CancellationToken cancellationToken)
    {
        actorId = ResolveActor(actorId);
        Snapshot = await _requestWorkspaceService.GetWorkspaceAsync(actorId, pageNumber ?? 1, cancellationToken);
        RequestTypeOptions = BuildRequestTypeOptions();
        ActiveRequest = ResolveActiveRequest(selectedRequestId);
        SelectedRequestId = ActiveRequest?.Id;

        if (Snapshot.ActiveActor is not null)
        {
            Input.RequestedByUserId = Snapshot.ActiveActor.Id;
        }

        SelectedWorkflowTemplate = await _requestWorkspaceService.GetWorkflowTemplateAsync(
            Input.GuaranteeNumber,
            Input.RequestType,
            cancellationToken);
    }

    private RequestSummaryDto? ResolveActiveRequest(Guid? selectedRequestId)
    {
        if (Snapshot.OwnedRequests.Count == 0)
        {
            return null;
        }

        if (selectedRequestId.HasValue)
        {
            var selectedRequest = Snapshot.OwnedRequests.FirstOrDefault(request => request.Id == selectedRequestId.Value);
            if (selectedRequest is not null)
            {
                return selectedRequest;
            }
        }

        return Snapshot.OwnedRequests[0];
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
            ? RedirectToPage("/Requests/Workspace", new { page = pageNumber, request = selectedRequestId })
            : RedirectToPage("/Requests/Workspace", new { actor = actorId, page = pageNumber, request = selectedRequestId });
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

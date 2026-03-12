using BG.Application.Approvals;
using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

[Authorize(Policy = PermissionPolicyNames.DelegationsManage)]
public sealed class DelegationsModel : PageModel
{
    private readonly IApprovalDelegationAdministrationService _approvalDelegationAdministrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DelegationsModel(
        IApprovalDelegationAdministrationService approvalDelegationAdministrationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _approvalDelegationAdministrationService = approvalDelegationAdministrationService;
        _localizer = localizer;
    }

    [BindProperty]
    public CreateApprovalDelegationInput Input { get; set; } = new();

    public ApprovalDelegationAdministrationSnapshotDto Snapshot { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!Input.StartsAtUtc.HasValue || !Input.EndsAtUtc.HasValue)
        {
            ModelState.AddModelError(string.Empty, _localizer[ApprovalDelegationErrorCodes.InvalidPeriod]);
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await _approvalDelegationAdministrationService.CreateAsync(
            new CreateApprovalDelegationCommand(
                Input.DelegatorUserId,
                Input.DelegateUserId,
                Input.RoleId,
                Input.StartsAtUtc.Value.ToUniversalTime(),
                Input.EndsAtUtc.Value.ToUniversalTime(),
                Input.Reason),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            await LoadAsync(cancellationToken);
            return Page();
        }

        StatusMessage = _localizer["ApprovalDelegation_CreateSuccess"];
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid delegationId, string? reason, CancellationToken cancellationToken)
    {
        var result = await _approvalDelegationAdministrationService.RevokeAsync(
            new RevokeApprovalDelegationCommand(delegationId, reason),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            await LoadAsync(cancellationToken);
            return Page();
        }

        StatusMessage = _localizer["ApprovalDelegation_RevokeSuccess"];
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Snapshot = await _approvalDelegationAdministrationService.GetSnapshotAsync(cancellationToken);
    }

    public sealed class CreateApprovalDelegationInput
    {
        public Guid DelegatorUserId { get; set; }

        public Guid DelegateUserId { get; set; }

        public Guid RoleId { get; set; }

        public DateTimeOffset? StartsAtUtc { get; set; }

        public DateTimeOffset? EndsAtUtc { get; set; }

        public string? Reason { get; set; }
    }
}

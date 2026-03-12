using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

[Authorize(Policy = PermissionPolicyNames.UsersManage)]
public sealed class UsersModel : PageModel
{
    private readonly IIdentityAdministrationService _identityAdministrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UsersModel(
        IIdentityAdministrationService identityAdministrationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _identityAdministrationService = identityAdministrationService;
        _localizer = localizer;
    }

    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public IReadOnlyList<UserSummaryDto> Users { get; private set; } = Array.Empty<UserSummaryDto>();

    public IReadOnlyList<RoleSummaryDto> Roles { get; private set; } = Array.Empty<RoleSummaryDto>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _identityAdministrationService.CreateUserAsync(
            new CreateUserCommand(
                Input.Username,
                Input.DisplayName,
                Input.Email,
                Input.InitialPassword,
                Input.SelectedRoleIds),
            cancellationToken);

        if (result.Succeeded)
        {
            StatusMessage = _localizer["UsersPage_UserCreated"];
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var result = await _identityAdministrationService.SetUserPasswordAsync(
            new SetLocalUserPasswordCommand(userId, newPassword),
            cancellationToken);

        if (result.Succeeded)
        {
            StatusMessage = _localizer["UsersPage_PasswordUpdated"];
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Users = await _identityAdministrationService.GetUsersAsync(cancellationToken);
        Roles = await _identityAdministrationService.GetRolesAsync(cancellationToken);
    }

    public sealed class CreateUserInput
    {
        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string InitialPassword { get; set; } = string.Empty;

        public List<Guid> SelectedRoleIds { get; set; } = [];
    }
}

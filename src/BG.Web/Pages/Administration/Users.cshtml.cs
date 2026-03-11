using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

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
                Input.SelectedRoleIds),
            cancellationToken);

        if (result.Succeeded)
        {
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

        public List<Guid> SelectedRoleIds { get; set; } = [];
    }
}

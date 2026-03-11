using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

public sealed class RolesModel : PageModel
{
    private readonly IIdentityAdministrationService _identityAdministrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RolesModel(
        IIdentityAdministrationService identityAdministrationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _identityAdministrationService = identityAdministrationService;
        _localizer = localizer;
    }

    [BindProperty]
    public CreateRoleInput Input { get; set; } = new();

    public IReadOnlyList<RoleSummaryDto> Roles { get; private set; } = Array.Empty<RoleSummaryDto>();

    public IReadOnlyList<PermissionGroupViewModel> PermissionGroups { get; private set; } = Array.Empty<PermissionGroupViewModel>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _identityAdministrationService.CreateRoleAsync(
            new CreateRoleCommand(
                Input.Name,
                Input.Description,
                Input.SelectedPermissionKeys),
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
        Roles = await _identityAdministrationService.GetRolesAsync(cancellationToken);
        var permissions = await _identityAdministrationService.GetPermissionsAsync(cancellationToken);
        PermissionGroups = permissions
            .GroupBy(permission => permission.Area)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PermissionGroupViewModel(
                group.Key,
                group.OrderBy(permission => permission.Key, StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();
    }

    public sealed class CreateRoleInput
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string> SelectedPermissionKeys { get; set; } = [];
    }

    public sealed record PermissionGroupViewModel(string Area, IReadOnlyList<PermissionDto> Permissions);
}

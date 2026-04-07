using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

[Authorize(Policy = PermissionPolicyNames.RolesManage)]
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

    [FromQuery(Name = "role")]
    public Guid? SelectedRoleId { get; set; }

    public IReadOnlyList<RoleSummaryDto> Roles { get; private set; } = Array.Empty<RoleSummaryDto>();

    public IReadOnlyList<PermissionGroupViewModel> PermissionGroups { get; private set; } = Array.Empty<PermissionGroupViewModel>();

    public RoleSummaryDto? ActiveRole { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

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
            StatusMessage = _localizer["RolesPage_RoleCreated"];
            return RedirectToPage(new { role = result.Value!.Id });
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public IDictionary<string, string> BuildSelectionRoute(Guid roleId)
    {
        return new Dictionary<string, string>
        {
            ["role"] = roleId.ToString()
        };
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
        ActiveRole = ResolveActiveRole(SelectedRoleId);
        SelectedRoleId = ActiveRole?.Id;

        if (ActiveRole != null && string.IsNullOrEmpty(Input.Name))
        {
            Input.Name = ActiveRole.Name;
            Input.Description = ActiveRole.Description;
            Input.SelectedPermissionKeys = ActiveRole.PermissionKeys.ToList();
        }
    }

    private RoleSummaryDto? ResolveActiveRole(Guid? selectedRoleId)
    {
        if (Roles.Count == 0)
        {
            return null;
        }

        if (selectedRoleId.HasValue)
        {
            var selectedRole = Roles.FirstOrDefault(role => role.Id == selectedRoleId.Value);
            if (selectedRole is not null)
            {
                return selectedRole;
            }
        }

        return Roles[0];
    }

    public sealed class CreateRoleInput
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string> SelectedPermissionKeys { get; set; } = [];
    }

    public sealed record PermissionGroupViewModel(string Area, IReadOnlyList<PermissionDto> Permissions);
}

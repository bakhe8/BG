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

    [FromQuery(Name = "user")]
    public Guid? SelectedUserId { get; set; }

    public IReadOnlyList<UserSummaryDto> Users { get; private set; } = Array.Empty<UserSummaryDto>();

    public IReadOnlyList<RoleSummaryDto> Roles { get; private set; } = Array.Empty<RoleSummaryDto>();

    public UserSummaryDto? ActiveUser { get; private set; }

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
            return RedirectToPage(new { user = result.Value!.Id });
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
            return RedirectToPage(new { user = result.Value!.Id });
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
        SelectedUserId = userId;
        await LoadAsync(cancellationToken);
        return Page();
    }

    public IDictionary<string, string> BuildSelectionRoute(Guid userId)
    {
        return new Dictionary<string, string>
        {
            ["user"] = userId.ToString()
        };
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Users = await _identityAdministrationService.GetUsersAsync(cancellationToken);
        Roles = await _identityAdministrationService.GetRolesAsync(cancellationToken);
        ActiveUser = ResolveActiveUser(SelectedUserId);
        SelectedUserId = ActiveUser?.Id;
    }

    private UserSummaryDto? ResolveActiveUser(Guid? selectedUserId)
    {
        if (Users.Count == 0)
        {
            return null;
        }

        if (selectedUserId.HasValue)
        {
            var selectedUser = Users.FirstOrDefault(user => user.Id == selectedUserId.Value);
            if (selectedUser is not null)
            {
                return selectedUser;
            }
        }

        return Users[0];
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

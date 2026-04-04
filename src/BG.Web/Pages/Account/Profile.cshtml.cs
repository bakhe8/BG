using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Localization;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Account;

[Authorize]
public sealed class ProfileModel : PageModel
{
    private readonly IUserAccessProfileService _userAccessProfileService;
    private readonly IWorkspaceShellService _shellService;
    private readonly IUiConfigurationService _uiConfigurationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ProfileModel(
        IUserAccessProfileService userAccessProfileService,
        IWorkspaceShellService shellService,
        IUiConfigurationService uiConfigurationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userAccessProfileService = userAccessProfileService;
        _shellService = shellService;
        _uiConfigurationService = uiConfigurationService;
        _localizer = localizer;
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public UserAccessProfileDto? Profile { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public List<SelectListItem> CultureOptions { get; private set; } = [];
    public List<SelectListItem> ThemeOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = _shellService.GetAuthenticatedUserId();
        if (!userId.HasValue) return RedirectToPage("/SignIn");

        Profile = await _userAccessProfileService.GetProfileAsync(userId.Value, cancellationToken);
        if (Profile is null) return RedirectToPage("/SignIn");

        Input = new ProfileInput
        {
            DisplayName = Profile.DisplayName,
            Email = Profile.Email,
            PreferredCulture = Profile.PreferredCulture ?? _uiConfigurationService.DefaultCulture,
            PreferredTheme = Profile.PreferredTheme ?? _uiConfigurationService.DefaultTheme
        };

        LoadOptions();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = _shellService.GetAuthenticatedUserId();
        if (!userId.HasValue) return RedirectToPage("/SignIn");

        if (!ModelState.IsValid)
        {
            Profile = await _userAccessProfileService.GetProfileAsync(userId.Value, cancellationToken);
            LoadOptions();
            return Page();
        }

        var result = await _userAccessProfileService.UpdateProfileAsync(
            userId.Value,
            new UpdateProfileCommand(
                Input.DisplayName,
                Input.Email,
                Input.PreferredCulture,
                Input.PreferredTheme),
            cancellationToken);

        if (result.Succeeded)
        {
            // Sync culture cookie so the page renders immediately in the new language
            if (!string.IsNullOrWhiteSpace(Input.PreferredCulture))
            {
                var normalizedCulture = _uiConfigurationService.NormalizeCulture(Input.PreferredCulture);
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    });
            }

            StatusMessage = _localizer["Profile_UpdateSuccess"];
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode ?? "Error_Generic"]);
        Profile = await _userAccessProfileService.GetProfileAsync(userId.Value, cancellationToken);
        LoadOptions();
        return Page();
    }

    private void LoadOptions()
    {
        CultureOptions = _uiConfigurationService.SupportedCultures
            .Select(c => new SelectListItem(c.Culture.ToUpperInvariant(), c.Culture))
            .ToList();

        ThemeOptions = _uiConfigurationService.SupportedThemes
            .Select(t => new SelectListItem(t.Key.ToUpperInvariant(), t.Key))
            .ToList();
    }

    public sealed class ProfileInput
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PreferredCulture { get; set; }
        public string? PreferredTheme { get; set; }
    }
}

using BG.Web.UI;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BG.Web.Controllers;

[Route("ui/preferences")]
public sealed class UiPreferencesController : Controller
{
    private readonly IUiConfigurationService _uiConfigurationService;

    public UiPreferencesController(IUiConfigurationService uiConfigurationService)
    {
        _uiConfigurationService = uiConfigurationService;
    }

    [HttpGet("culture")]
    public IActionResult SetCulture(string culture, string? returnUrl = "/")
    {
        var normalizedCulture = _uiConfigurationService.NormalizeCulture(culture);
        var safeReturnUrl = _uiConfigurationService.EnsureLocalReturnUrl(returnUrl);

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(safeReturnUrl);
    }

    [HttpGet("theme")]
    public IActionResult SetTheme(string theme, string? returnUrl = "/")
    {
        var normalizedTheme = _uiConfigurationService.NormalizeTheme(theme);
        var safeReturnUrl = _uiConfigurationService.EnsureLocalReturnUrl(returnUrl);

        Response.Cookies.Append(
            UiConfigurationService.ThemeCookieName,
            normalizedTheme,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(safeReturnUrl);
    }
}

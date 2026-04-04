using System.Globalization;

namespace BG.Web.UI;

public sealed class UiConfigurationService : IUiConfigurationService
{
    public const string ThemeCookieName = "bg-theme";
    public const string DefaultCultureName = "ar";
    public const string DefaultThemeKey = "kfsh";

    private static readonly UiCultureOption[] CultureOptions =
    {
        new("ar"),
        new("en")
    };

    private static readonly UiThemeOption[] ThemeOptions =
    {
        new("kfsh")
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWorkspaceShellService _shellService;

    public UiConfigurationService(
        IHttpContextAccessor httpContextAccessor,
        IWorkspaceShellService shellService)
    {
        _httpContextAccessor = httpContextAccessor;
        _shellService = shellService;
    }

    public IReadOnlyList<UiCultureOption> SupportedCultures => CultureOptions;

    public IReadOnlyList<UiThemeOption> SupportedThemes => ThemeOptions;

    public string DefaultCulture => DefaultCultureName;

    public string DefaultTheme => DefaultThemeKey;

    public string GetCurrentCulture()
    {
        return NormalizeCulture(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    public string GetCurrentTheme()
    {
        // 1. Check session snapshot for authenticated user preference
        var shellTask = _shellService.GetSnapshotAsync();
        if (shellTask.IsCompletedSuccessfully && shellTask.Result.CurrentUser?.PreferredTheme is { } preferred)
        {
            return NormalizeTheme(preferred);
        }

        // 2. Fallback to cookie for anonymous or unsynced state
        var theme = _httpContextAccessor.HttpContext?.Request.Cookies[ThemeCookieName];
        return NormalizeTheme(theme);
    }

    public string GetDirection(string? culture = null)
    {
        var normalizedCulture = NormalizeCulture(culture);
        var cultureInfo = CultureInfo.GetCultureInfo(normalizedCulture);
        return cultureInfo.TextInfo.IsRightToLeft ? "rtl" : "ltr";
    }

    public string NormalizeCulture(string? culture)
    {
        var candidate = string.IsNullOrWhiteSpace(culture)
            ? DefaultCultureName
            : culture.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

        return CultureOptions.Any(option => option.Culture == candidate)
            ? candidate
            : DefaultCultureName;
    }

    public string NormalizeTheme(string? theme)
    {
        var candidate = string.IsNullOrWhiteSpace(theme)
            ? DefaultThemeKey
            : theme.Trim().ToLowerInvariant();

        return ThemeOptions.Any(option => option.Key == candidate)
            ? candidate
            : DefaultThemeKey;
    }

    public string EnsureLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            return "/";
        }

        return returnUrl.StartsWith('/') ? returnUrl : "/";
    }
}

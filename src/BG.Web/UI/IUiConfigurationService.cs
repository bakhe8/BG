namespace BG.Web.UI;

public interface IUiConfigurationService
{
    IReadOnlyList<UiCultureOption> SupportedCultures { get; }

    IReadOnlyList<UiThemeOption> SupportedThemes { get; }

    string DefaultCulture { get; }

    string DefaultTheme { get; }

    string GetCurrentCulture();

    string GetCurrentTheme();

    string GetDirection(string? culture = null);

    string NormalizeCulture(string? culture);

    string NormalizeTheme(string? theme);

    string EnsureLocalReturnUrl(string? returnUrl);
}

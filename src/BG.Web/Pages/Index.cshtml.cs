using System.Globalization;
using BG.Application.Contracts.Services;
using BG.Application.Models;
using BG.Integrations.Options;
using BG.Web.UI;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BG.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IArchitectureProfileService _architectureProfileService;
    private readonly IOptions<HospitalApiOptions> _hospitalApiOptions;
    private readonly IUiConfigurationService _uiConfigurationService;

    public IndexModel(
        IArchitectureProfileService architectureProfileService,
        IOptions<HospitalApiOptions> hospitalApiOptions,
        IUiConfigurationService uiConfigurationService)
    {
        _architectureProfileService = architectureProfileService;
        _hospitalApiOptions = hospitalApiOptions;
        _uiConfigurationService = uiConfigurationService;
    }

    public ArchitectureProfileDto Architecture { get; private set; } = default!;

    public bool HospitalApiConfigured { get; private set; }

    public string? HospitalApiBaseUrl { get; private set; }

    public string HospitalApiAuthMode { get; private set; } = string.Empty;

    public string CurrentCulture { get; private set; } = string.Empty;

    public string CurrentDirection { get; private set; } = string.Empty;

    public string CurrentTheme { get; private set; } = string.Empty;

    public IReadOnlyList<UiCultureOption> SupportedCultures { get; private set; } = Array.Empty<UiCultureOption>();

    public IReadOnlyList<UiThemeOption> SupportedThemes { get; private set; } = Array.Empty<UiThemeOption>();

    public string ReturnUrl { get; private set; } = "/";

    public void OnGet()
    {
        Architecture = _architectureProfileService.GetCurrent();

        var hospitalApi = _hospitalApiOptions.Value;
        HospitalApiConfigured = Uri.TryCreate(hospitalApi.BaseUrl, UriKind.Absolute, out _);
        HospitalApiBaseUrl = string.IsNullOrWhiteSpace(hospitalApi.BaseUrl) ? null : hospitalApi.BaseUrl;
        HospitalApiAuthMode = hospitalApi.AuthenticationMode;
        CurrentCulture = _uiConfigurationService.GetCurrentCulture();
        CurrentDirection = _uiConfigurationService.GetDirection(CultureInfo.CurrentUICulture.Name);
        CurrentTheme = _uiConfigurationService.GetCurrentTheme();
        SupportedCultures = _uiConfigurationService.SupportedCultures;
        SupportedThemes = _uiConfigurationService.SupportedThemes;
        ReturnUrl = Request.Path + Request.QueryString;
    }
}

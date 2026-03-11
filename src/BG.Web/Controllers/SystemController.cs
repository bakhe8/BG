using BG.Application.Contracts.Services;
using BG.Integrations.Options;
using BG.Web.Contracts.System;
using BG.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BG.Web.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    private readonly IArchitectureProfileService _architectureProfileService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptions<HospitalApiOptions> _hospitalApiOptions;
    private readonly IUiConfigurationService _uiConfigurationService;

    public SystemController(
        IArchitectureProfileService architectureProfileService,
        IHostEnvironment hostEnvironment,
        IOptions<HospitalApiOptions> hospitalApiOptions,
        IUiConfigurationService uiConfigurationService)
    {
        _architectureProfileService = architectureProfileService;
        _hostEnvironment = hostEnvironment;
        _hospitalApiOptions = hospitalApiOptions;
        _uiConfigurationService = uiConfigurationService;
    }

    [HttpGet("ping")]
    public ActionResult<SystemPingResponse> Ping()
    {
        return Ok(new SystemPingResponse(
            "ok",
            _hostEnvironment.EnvironmentName,
            DateTimeOffset.UtcNow));
    }

    [HttpGet("architecture")]
    public ActionResult<SystemArchitectureResponse> GetArchitecture()
    {
        var profile = _architectureProfileService.GetCurrent();
        var hospitalApi = _hospitalApiOptions.Value;

        return Ok(new SystemArchitectureResponse(
            profile.ApplicationName,
            profile.Framework,
            profile.UserInterface,
            profile.ApiStyle,
            profile.Database,
            profile.Hosting,
            profile.IntegrationApproach,
            _uiConfigurationService.DefaultCulture,
            _uiConfigurationService.SupportedCultures.Select(option => option.Culture).ToArray(),
            _uiConfigurationService.DefaultTheme,
            _uiConfigurationService.SupportedThemes.Select(option => option.Key).ToArray(),
            new HospitalApiConfigurationResponse(
                Uri.TryCreate(hospitalApi.BaseUrl, UriKind.Absolute, out _),
                hospitalApi.BaseUrl,
                hospitalApi.AuthenticationMode,
                hospitalApi.TimeoutSeconds)));
    }
}

using BG.Application.Contracts.Services;
using BG.Web.Contracts.System;
using BG.Web.UI;
using Microsoft.AspNetCore.Mvc;

namespace BG.Web.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    private readonly IArchitectureProfileService _architectureProfileService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IUiConfigurationService _uiConfigurationService;

    public SystemController(
        IArchitectureProfileService architectureProfileService,
        IHostEnvironment hostEnvironment,
        IUiConfigurationService uiConfigurationService)
    {
        _architectureProfileService = architectureProfileService;
        _hostEnvironment = hostEnvironment;
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
            _uiConfigurationService.SupportedThemes.Select(option => option.Key).ToArray()));
    }
}

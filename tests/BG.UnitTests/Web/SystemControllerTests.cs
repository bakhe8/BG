using BG.Application.Models;
using BG.Integrations.Options;
using BG.Web.Controllers;
using BG.Web.Contracts.System;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BG.UnitTests.Web;

public sealed class SystemControllerTests
{
    [Fact]
    public void Ping_returns_ok_status_and_current_environment()
    {
        var controller = CreateController(new HospitalApiOptions(), "Test");

        var result = controller.Ping();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SystemPingResponse>(okResult.Value);

        Assert.Equal("ok", response.Status);
        Assert.Equal("Test", response.Environment);
        Assert.True(response.UtcNow <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GetArchitecture_returns_profile_and_hospital_api_configuration_state()
    {
        var controller = CreateController(
            new HospitalApiOptions
            {
                BaseUrl = "https://hospital-api.internal/",
                TimeoutSeconds = 45,
                AuthenticationMode = "ApiKey",
                ApiKeyHeaderName = "X-Api-Key"
            },
            "Development");

        var result = controller.GetArchitecture();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SystemArchitectureResponse>(okResult.Value);

        Assert.Equal("BG", response.ApplicationName);
        Assert.Equal("ASP.NET Core 8", response.Framework);
        Assert.Equal("Razor Pages", response.UserInterface);
        Assert.Equal("ar", response.DefaultCulture);
        Assert.Equal("kfsh", response.DefaultTheme);
        Assert.Contains("ar", response.SupportedCultures);
        Assert.Contains("en", response.SupportedCultures);
        Assert.Single(response.SupportedThemes);
        Assert.Contains("kfsh", response.SupportedThemes);
        Assert.True(response.HospitalApi.IsConfigured);
        Assert.Equal("https://hospital-api.internal/", response.HospitalApi.BaseUrl);
        Assert.Equal("ApiKey", response.HospitalApi.AuthenticationMode);
        Assert.Equal(45, response.HospitalApi.TimeoutSeconds);
    }

    private static SystemController CreateController(
        HospitalApiOptions hospitalApiOptions,
        string environmentName)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        return new SystemController(
            new StubArchitectureProfileService(),
            new TestHostEnvironment(environmentName),
            Options.Create(hospitalApiOptions),
            new UiConfigurationService(httpContextAccessor));
    }

    private sealed class StubArchitectureProfileService : BG.Application.Contracts.Services.IArchitectureProfileService
    {
        public ArchitectureProfileDto GetCurrent()
        {
            return new ArchitectureProfileDto(
                "BG",
                "ASP.NET Core 8",
                "Razor Pages",
                "REST API Controllers",
                "PostgreSQL",
                "IIS",
                "Hospital API Integration Layer");
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "BG.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

using BG.Application;
using BG.Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BG.UnitTests.Application;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_registers_architecture_profile_service_with_expected_baseline()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IArchitectureProfileService>();

        var profile = service.GetCurrent();

        Assert.Equal("BG", profile.ApplicationName);
        Assert.Equal("ASP.NET Core 8", profile.Framework);
        Assert.Equal("Razor Pages", profile.UserInterface);
        Assert.Equal("REST API Controllers", profile.ApiStyle);
        Assert.Equal("PostgreSQL", profile.Database);
        Assert.Equal("IIS", profile.Hosting);
        Assert.Equal("Hospital API Integration Layer", profile.IntegrationApproach);
    }

    [Fact]
    public void AddApplication_registers_identity_administration_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IIdentityAdministrationService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_intake_workspace_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IIntakeWorkspaceService)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}

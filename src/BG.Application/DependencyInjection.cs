using BG.Application.Contracts.Services;
using BG.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IArchitectureProfileService, ArchitectureProfileService>();
        services.AddSingleton<IIntakeWorkspaceService, IntakeWorkspaceService>();
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();

        return services;
    }
}

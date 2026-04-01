using BG.Application.Contracts.Services;
using BG.Integrations.Services;
using BG.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BG.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LocalOcrOptions>(
            configuration.GetSection(LocalOcrOptions.SectionName));
        services.AddSingleton<ILocalOcrWorkerRunner, LocalPythonOcrProcessingService>();
        services.AddSingleton<QueuedOcrProcessingService>();
        services.AddSingleton<IOcrDocumentProcessingService>(serviceProvider => serviceProvider.GetRequiredService<QueuedOcrProcessingService>());
        services.AddSingleton<IHostedService>(serviceProvider => serviceProvider.GetRequiredService<QueuedOcrProcessingService>());

        return services;
    }
}

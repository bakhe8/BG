using System.Net.Http.Headers;
using BG.Application.Contracts.Services;
using BG.Integrations.Services;
using BG.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BG.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HospitalApiOptions>(
            configuration.GetSection(HospitalApiOptions.SectionName));
        services.Configure<LocalOcrOptions>(
            configuration.GetSection(LocalOcrOptions.SectionName));
        services.AddSingleton<IOcrDocumentProcessingService, LocalPythonOcrProcessingService>();

        services.AddHttpClient("HospitalApi", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<HospitalApiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 300));
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            if (string.Equals(options.AuthenticationMode, "ApiKey", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(options.ApiKey) &&
                !string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    options.ApiKeyHeaderName,
                    options.ApiKey);
            }
        });

        return services;
    }
}

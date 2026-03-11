namespace BG.Integrations.Options;

public sealed class HospitalApiOptions
{
    public const string SectionName = "HospitalApi";

    public string? BaseUrl { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public string AuthenticationMode { get; init; } = "ApiKey";

    public string ApiKeyHeaderName { get; init; } = "X-Api-Key";

    public string? ApiKey { get; init; }
}

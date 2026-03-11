namespace BG.Web.Contracts.System;

public sealed record HospitalApiConfigurationResponse(
    bool IsConfigured,
    string? BaseUrl,
    string AuthenticationMode,
    int TimeoutSeconds);

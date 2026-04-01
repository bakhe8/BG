namespace BG.Web.Contracts.System;

public sealed record SystemArchitectureResponse(
    string ApplicationName,
    string Framework,
    string UserInterface,
    string ApiStyle,
    string Database,
    string Hosting,
    string IntegrationApproach,
    string DefaultCulture,
    IReadOnlyList<string> SupportedCultures,
    string DefaultTheme,
    IReadOnlyList<string> SupportedThemes);

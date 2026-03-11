namespace BG.Application.Models;

public sealed record ArchitectureProfileDto(
    string ApplicationName,
    string Framework,
    string UserInterface,
    string ApiStyle,
    string Database,
    string Hosting,
    string IntegrationApproach);

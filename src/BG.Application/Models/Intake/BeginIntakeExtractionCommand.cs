namespace BG.Application.Models.Intake;

public sealed record BeginIntakeExtractionCommand(
    string ScenarioKey,
    string FileName,
    Stream Content);

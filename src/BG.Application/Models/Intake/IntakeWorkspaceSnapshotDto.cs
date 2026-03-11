namespace BG.Application.Models.Intake;

public sealed record IntakeWorkspaceSnapshotDto(
    string PrimaryRoleResourceKey,
    string SaveModeResourceKey,
    string ReviewGateResourceKey,
    IReadOnlyList<string> AllowedActionKeys,
    IReadOnlyList<string> ExcludedActionKeys,
    IReadOnlyList<string> PipelineStepKeys,
    IReadOnlyList<string> QualityGateKeys,
    IReadOnlyList<string> FutureIntegrationKeys,
    IReadOnlyList<IntakeScenarioSnapshotDto> Scenarios,
    IntakeScenarioSnapshotDto SelectedScenario);

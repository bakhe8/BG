namespace BG.Application.Models.Intake;

public sealed record IntakeWorkspaceSnapshotDto(
    IntakeActorSummaryDto? ActiveActor,
    IReadOnlyList<IntakeActorSummaryDto> AvailableActors,
    string PrimaryRoleResourceKey,
    string SaveModeResourceKey,
    string ReviewGateResourceKey,
    IReadOnlyList<string> AllowedActionKeys,
    IReadOnlyList<string> ExcludedActionKeys,
    IReadOnlyList<string> PipelineStepKeys,
    IReadOnlyList<string> QualityGateKeys,
    IReadOnlyList<string> FutureIntegrationKeys,
    IReadOnlyList<IntakeScenarioSnapshotDto> Scenarios,
    IntakeScenarioSnapshotDto SelectedScenario,
    bool HasEligibleActor,
    string? ContextNoticeResourceKey);

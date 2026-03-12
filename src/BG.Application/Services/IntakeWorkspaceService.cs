using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Domain.Identity;

namespace BG.Application.Services;

internal sealed class IntakeWorkspaceService : IIntakeWorkspaceService
{
    private readonly IIntakeRepository _repository;

    public IntakeWorkspaceService(IIntakeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IntakeWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? intakeActorId = null,
        string? scenarioKey = null,
        CancellationToken cancellationToken = default)
    {
        var actors = await _repository.ListIntakeActorsAsync(cancellationToken);

        if (actors.Count == 0)
        {
            var fallbackScenarios = IntakeScenarioCatalog.GetAll()
                .Select(MapScenario)
                .ToArray();

            return new IntakeWorkspaceSnapshotDto(
                null,
                [],
                "IntakeWorkspace_PrimaryRole",
                "IntakeWorkspace_SaveMode",
                "IntakeWorkspace_ReviewGate",
                [
                    "IntakeAction_Scan",
                    "IntakeAction_Classify",
                    "IntakeAction_Verify",
                    "IntakeAction_Save"
                ],
                [
                    "IntakeExcluded_RequestActions",
                    "IntakeExcluded_Printing",
                    "IntakeExcluded_Approvals",
                    "IntakeExcluded_Dispatch"
                ],
                [
                    "IntakePipeline_ClassifyPage",
                    "IntakePipeline_DirectPdfText",
                    "IntakePipeline_ImagePreprocessing",
                    "IntakePipeline_OcrAndLayout",
                    "IntakePipeline_PostProcessing",
                    "IntakePipeline_HumanReview",
                    "IntakePipeline_SaveAndHandoff"
                ],
                [
                    "IntakeQuality_TextFirst",
                    "IntakeQuality_Confidence",
                    "IntakeQuality_SingleSave",
                    "IntakeQuality_SourceDocument",
                    "IntakeQuality_BankSpecificRules"
                ],
                [
                    "IntakeFuture_ScanStation",
                    "IntakeFuture_ScanToFolder",
                    "IntakeFuture_DeviceApi"
                ],
                fallbackScenarios,
                fallbackScenarios[0],
                false,
                "IntakeWorkspace_NoEligibleActor");
        }

        var activeActor = intakeActorId.HasValue
            ? actors.FirstOrDefault(actor => actor.Id == intakeActorId.Value)
            : actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        activeActor ??= actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        var scenarios = IntakeScenarioCatalog.GetAll()
            .Select(MapScenario)
            .ToArray();

        var selectedScenario = scenarios.FirstOrDefault(
                scenario => string.Equals(scenario.Key, scenarioKey, StringComparison.OrdinalIgnoreCase))
            ?? scenarios[0];

        return new IntakeWorkspaceSnapshotDto(
            MapActor(activeActor),
            actors
                .OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(MapActor)
                .ToArray(),
            "IntakeWorkspace_PrimaryRole",
            "IntakeWorkspace_SaveMode",
            "IntakeWorkspace_ReviewGate",
            [
                "IntakeAction_Scan",
                "IntakeAction_Classify",
                "IntakeAction_Verify",
                "IntakeAction_Save"
            ],
            [
                "IntakeExcluded_RequestActions",
                "IntakeExcluded_Printing",
                "IntakeExcluded_Approvals",
                "IntakeExcluded_Dispatch"
            ],
            [
                "IntakePipeline_ClassifyPage",
                "IntakePipeline_DirectPdfText",
                "IntakePipeline_ImagePreprocessing",
                "IntakePipeline_OcrAndLayout",
                "IntakePipeline_PostProcessing",
                "IntakePipeline_HumanReview",
                "IntakePipeline_SaveAndHandoff"
            ],
            [
                "IntakeQuality_TextFirst",
                "IntakeQuality_Confidence",
                "IntakeQuality_SingleSave",
                "IntakeQuality_SourceDocument",
                "IntakeQuality_BankSpecificRules"
            ],
            [
                "IntakeFuture_ScanStation",
                "IntakeFuture_ScanToFolder",
                "IntakeFuture_DeviceApi"
            ],
            scenarios,
            selectedScenario,
            true,
            "IntakeWorkspace_ActorScopedNotice");
    }

    private static IntakeScenarioSnapshotDto MapScenario(IntakeScenarioDefinition definition)
    {
        return new IntakeScenarioSnapshotDto(
            definition.Key,
            definition.TitleResourceKey,
            definition.SummaryResourceKey,
            definition.SaveOutcomeResourceKey,
            definition.HandoffResourceKey,
            definition.RequiredReviewFieldKeys,
            definition.SampleFields,
            definition.RequiresExistingGuarantee,
            definition.RequiresConfirmedExpiryDate,
            definition.RequiresConfirmedAmount,
            definition.RequiresStatusStatement,
            definition.RequiresAttachmentNote);
    }

    private static IntakeActorSummaryDto MapActor(User actor)
    {
        return new IntakeActorSummaryDto(actor.Id, actor.Username, actor.DisplayName);
    }
}

using BG.Application.Models.Intake;

namespace BG.Application.Intake;

internal interface IIntakeOcrExtractor
{
    Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        CancellationToken cancellationToken = default);
}

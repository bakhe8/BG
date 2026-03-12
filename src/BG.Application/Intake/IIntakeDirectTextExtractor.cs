using BG.Application.Models.Intake;

namespace BG.Application.Intake;

internal interface IIntakeDirectTextExtractor
{
    Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        CancellationToken cancellationToken = default);
}

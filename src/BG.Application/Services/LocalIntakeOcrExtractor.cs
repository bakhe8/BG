using BG.Application.Intake;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal sealed class LocalIntakeOcrExtractor : IIntakeOcrExtractor
{
    public Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            LocalIntakeExtractionHeuristics.CreateCandidates(
                scenario,
                stagedDocument,
                IntakeFieldValueSource.OcrFallback));
    }
}

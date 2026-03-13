using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Services;

internal sealed class LocalIntakeOcrExtractor : IIntakeOcrExtractor
{
    public Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        GuaranteeDocumentFormDefinition documentForm,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            LocalIntakeExtractionHeuristics.CreateCandidates(
                scenario,
                stagedDocument,
                documentForm,
                IntakeFieldValueSource.OcrFallback));
    }
}

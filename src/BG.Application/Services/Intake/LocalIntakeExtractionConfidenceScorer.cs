using BG.Application.Intake;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal sealed class LocalIntakeExtractionConfidenceScorer : IIntakeExtractionConfidenceScorer
{
    public int Score(IntakeFieldReviewDto sampleField, IntakeExtractionFieldCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(sampleField);
        ArgumentNullException.ThrowIfNull(candidate);

        var boundedConfidence = Math.Clamp(candidate.ConfidencePercent, 0, 100);

        return candidate.Source switch
        {
            IntakeFieldValueSource.FilenamePattern => Math.Min(boundedConfidence, 65),
            IntakeFieldValueSource.DirectPdfText => Math.Max(boundedConfidence, 0),
            IntakeFieldValueSource.OcrFallback => Math.Max(boundedConfidence, 0),
            IntakeFieldValueSource.ScenarioSample => Math.Min(boundedConfidence, 25),
            _ => boundedConfidence
        };
    }
}

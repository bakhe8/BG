using BG.Application.Intake;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal sealed class LocalIntakeExtractionConfidenceScorer : IIntakeExtractionConfidenceScorer
{
    public int Score(IntakeFieldReviewDto sampleField, IntakeFieldValueSource source)
    {
        ArgumentNullException.ThrowIfNull(sampleField);

        return source switch
        {
            IntakeFieldValueSource.FilenamePattern => 99,
            IntakeFieldValueSource.DirectPdfText => Math.Max(sampleField.ConfidencePercent, 95),
            IntakeFieldValueSource.OcrFallback => Math.Max(sampleField.ConfidencePercent, 90),
            IntakeFieldValueSource.ScenarioSample => sampleField.ConfidencePercent,
            _ => sampleField.ConfidencePercent
        };
    }
}

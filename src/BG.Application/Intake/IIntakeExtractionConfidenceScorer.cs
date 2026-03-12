using BG.Application.Models.Intake;

namespace BG.Application.Intake;

internal interface IIntakeExtractionConfidenceScorer
{
    int Score(IntakeFieldReviewDto sampleField, IntakeFieldValueSource source);
}

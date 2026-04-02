using BG.Application.Models.Intake;

namespace BG.Application.Intake;

internal interface IIntakeExtractionConfidenceScorer
{
    int Score(IntakeFieldReviewDto sampleField, IntakeExtractionFieldCandidate candidate);
}

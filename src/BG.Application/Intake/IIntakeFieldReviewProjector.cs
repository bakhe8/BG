using BG.Application.Models.Intake;

namespace BG.Application.Intake;

internal interface IIntakeFieldReviewProjector
{
    IReadOnlyList<IntakeFieldReviewDto> Project(
        IntakeScenarioDefinition scenario,
        IEnumerable<IntakeExtractionFieldCandidate> candidates);
}

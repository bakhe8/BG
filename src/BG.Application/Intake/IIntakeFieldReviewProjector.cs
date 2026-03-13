using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Intake;

internal interface IIntakeFieldReviewProjector
{
    IReadOnlyList<IntakeFieldReviewDto> Project(
        IntakeScenarioDefinition scenario,
        GuaranteeDocumentFormDefinition documentForm,
        IEnumerable<IntakeExtractionFieldCandidate> candidates);
}

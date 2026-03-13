using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Intake;

internal interface IIntakeOcrExtractor
{
    Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        GuaranteeDocumentFormDefinition documentForm,
        CancellationToken cancellationToken = default);
}

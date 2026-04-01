using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IIntakeExtractionEngine
{
    Task<IntakeExtractionDraftDto> ExtractAsync(
        string scenarioKey,
        StagedIntakeDocumentDto stagedDocument,
        CancellationToken cancellationToken = default);
}

using BG.Application.Common;
using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IIntakeSubmissionService
{
    Task<OperationResult<IntakeExtractionDraftDto>> BeginExtractionAsync(
        BeginIntakeExtractionCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<IntakeSubmissionReceiptDto>> FinalizeAsync(
        IntakeSubmissionCommand command,
        CancellationToken cancellationToken = default);
}

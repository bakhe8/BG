using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IOcrDocumentProcessingService
{
    Task<OcrDocumentProcessingResult> ProcessAsync(
        OcrDocumentProcessingRequest request,
        CancellationToken cancellationToken = default);
}

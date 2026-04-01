using BG.Application.Models.Intake;

namespace BG.Integrations.Services;

internal interface ILocalOcrWorkerRunner
{
    Task<OcrDocumentProcessingResult> ProcessAsync(
        OcrDocumentProcessingRequest request,
        CancellationToken cancellationToken = default);
}

using BG.Application.Contracts.Services;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal sealed class NullOcrDocumentProcessingService : IOcrDocumentProcessingService
{
    public Task<OcrDocumentProcessingResult> ProcessAsync(
        OcrDocumentProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(OcrDocumentProcessingResult.Disabled("bg-null-ocr"));
    }
}

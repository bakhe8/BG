namespace BG.Application.Models.Intake;

public sealed record OcrDocumentProcessingResult(
    bool Succeeded,
    string ProcessorName,
    string PipelineVersion,
    IReadOnlyList<OcrDocumentFieldCandidateDto> Fields,
    IReadOnlyList<string> Warnings,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static OcrDocumentProcessingResult Disabled(string processorName)
    {
        return new OcrDocumentProcessingResult(
            false,
            processorName,
            "disabled",
            [],
            [],
            "ocr.disabled",
            "OCR processing is disabled.");
    }
}

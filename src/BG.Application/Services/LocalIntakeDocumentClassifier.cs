using BG.Application.Intake;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal sealed class LocalIntakeDocumentClassifier : IIntakeDocumentClassifier
{
    public IntakeDocumentClassificationResult Classify(StagedIntakeDocumentDto stagedDocument)
    {
        ArgumentNullException.ThrowIfNull(stagedDocument);

        var isPdf = Path.GetExtension(stagedDocument.OriginalFileName)
            .Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        return isPdf
            ? new IntakeDocumentClassificationResult(
                IntakeExtractionStrategy.DirectTextFirst,
                IntakeExtractionRouteKeys.PdfTextFirst,
                PageCount: 1)
            : new IntakeDocumentClassificationResult(
                IntakeExtractionStrategy.OcrOnly,
                IntakeExtractionRouteKeys.OcrFallback,
                PageCount: 1);
    }
}

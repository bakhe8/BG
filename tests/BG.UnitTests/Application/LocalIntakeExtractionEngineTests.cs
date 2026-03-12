using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.Services;

namespace BG.UnitTests.Application;

public sealed class LocalIntakeExtractionEngineTests
{
    [Fact]
    public async Task ExtractAsync_for_pdf_uses_pdf_text_route_and_filename_provenance()
    {
        var engine = new LocalIntakeExtractionEngine();

        var draft = await engine.ExtractAsync(
            IntakeScenarioKeys.NewGuarantee,
            new StagedIntakeDocumentDto("token-1234", "BG-2026-0451.pdf", 256));

        Assert.Equal(IntakeExtractionRouteKeys.PdfTextFirst, draft.ExtractionRouteResourceKey);

        var guaranteeNumber = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber));
        Assert.Equal("BG-2026-0451", guaranteeNumber.Value);
        Assert.Equal("IntakeFieldProvenance_FilenamePattern", guaranteeNumber.ProvenanceResourceKey);
    }

    [Fact]
    public async Task ExtractAsync_for_image_uses_ocr_route_and_ocr_provenance()
    {
        var engine = new LocalIntakeExtractionEngine();

        var draft = await engine.ExtractAsync(
            IntakeScenarioKeys.ExtensionConfirmation,
            new StagedIntakeDocumentDto("token-5678", "incoming-scan.jpg", 256));

        Assert.Equal(IntakeExtractionRouteKeys.OcrFallback, draft.ExtractionRouteResourceKey);

        var officialLetterDate = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.OfficialLetterDate));
        Assert.Equal("IntakeFieldProvenance_OcrFallback", officialLetterDate.ProvenanceResourceKey);
    }
}

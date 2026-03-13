using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
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
            new StagedIntakeDocumentDto("token-1234", "snb_BG-2026-0451.pdf", 256));

        Assert.Equal(IntakeExtractionRouteKeys.PdfTextFirst, draft.ExtractionRouteResourceKey);
        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb, draft.DocumentFormKey);

        var guaranteeNumber = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber));
        Assert.Equal("BG-2026-0451", guaranteeNumber.Value);
        Assert.Equal("IntakeFieldProvenance_FilenamePattern", guaranteeNumber.ProvenanceResourceKey);
        Assert.True(guaranteeNumber.IsExpectedByDocumentForm);

        var bankName = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.BankName));
        Assert.Equal("Saudi National Bank", bankName.Value);
        Assert.Equal("IntakeFieldProvenance_DirectPdfText", bankName.ProvenanceResourceKey);
        Assert.True(bankName.IsExpectedByDocumentForm);

        var guaranteeCategory = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.GuaranteeCategory));
        Assert.True(guaranteeCategory.IsExpectedByDocumentForm);
        Assert.Equal("IntakeFieldProvenance_DirectPdfText", guaranteeCategory.ProvenanceResourceKey);
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
        Assert.True(officialLetterDate.IsExpectedByDocumentForm);

        var newExpiryDate = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.NewExpiryDate));
        Assert.False(newExpiryDate.IsExpectedByDocumentForm);
        Assert.Equal("IntakeFieldProvenance_ScenarioSample", newExpiryDate.ProvenanceResourceKey);
    }

    [Fact]
    public async Task ExtractAsync_for_generic_instrument_keeps_non_pinned_fields_as_scenario_samples()
    {
        var engine = new LocalIntakeExtractionEngine();

        var draft = await engine.ExtractAsync(
            IntakeScenarioKeys.NewGuarantee,
            new StagedIntakeDocumentDto("token-9012", "incoming-guarantee.pdf", 256));

        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric, draft.DocumentFormKey);

        var beneficiary = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.Beneficiary));
        Assert.False(beneficiary.IsExpectedByDocumentForm);
        Assert.Equal("IntakeFieldProvenance_ScenarioSample", beneficiary.ProvenanceResourceKey);

        var amount = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.Amount));
        Assert.True(amount.IsExpectedByDocumentForm);
        Assert.Equal("IntakeFieldProvenance_DirectPdfText", amount.ProvenanceResourceKey);
    }
}

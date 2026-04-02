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
        Assert.Equal(55, guaranteeNumber.ConfidencePercent);

        var bankName = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.BankName));
        Assert.Equal(string.Empty, bankName.Value);
        Assert.Null(bankName.ProvenanceResourceKey);
        Assert.True(bankName.IsExpectedByDocumentForm);
        Assert.Equal(0, bankName.ConfidencePercent);

        var guaranteeCategory = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.GuaranteeCategory));
        Assert.True(guaranteeCategory.IsExpectedByDocumentForm);
        Assert.Equal(string.Empty, guaranteeCategory.Value);
        Assert.Null(guaranteeCategory.ProvenanceResourceKey);
        Assert.Equal(0, guaranteeCategory.ConfidencePercent);
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
        Assert.Equal(string.Empty, officialLetterDate.Value);
        Assert.Null(officialLetterDate.ProvenanceResourceKey);
        Assert.True(officialLetterDate.IsExpectedByDocumentForm);
        Assert.Equal(0, officialLetterDate.ConfidencePercent);

        var newExpiryDate = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.NewExpiryDate));
        Assert.False(newExpiryDate.IsExpectedByDocumentForm);
        Assert.Equal(string.Empty, newExpiryDate.Value);
        Assert.Null(newExpiryDate.ProvenanceResourceKey);
        Assert.Equal(0, newExpiryDate.ConfidencePercent);
    }

    [Fact]
    public async Task ExtractAsync_for_generic_instrument_leaves_unconfirmed_fields_blank()
    {
        var engine = new LocalIntakeExtractionEngine();

        var draft = await engine.ExtractAsync(
            IntakeScenarioKeys.NewGuarantee,
            new StagedIntakeDocumentDto("token-9012", "incoming-guarantee.pdf", 256));

        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric, draft.DocumentFormKey);

        var beneficiary = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.Beneficiary));
        Assert.False(beneficiary.IsExpectedByDocumentForm);
        Assert.Equal(string.Empty, beneficiary.Value);
        Assert.Null(beneficiary.ProvenanceResourceKey);
        Assert.Equal(0, beneficiary.ConfidencePercent);

        var amount = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.Amount));
        Assert.True(amount.IsExpectedByDocumentForm);
        Assert.Equal(string.Empty, amount.Value);
        Assert.Null(amount.ProvenanceResourceKey);
        Assert.Equal(0, amount.ConfidencePercent);
    }

    [Fact]
    public async Task ExtractAsync_detects_bsf_instrument_family_from_filename()
    {
        var engine = new LocalIntakeExtractionEngine();

        var draft = await engine.ExtractAsync(
            IntakeScenarioKeys.NewGuarantee,
            new StagedIntakeDocumentDto("token-bsf1", "bsf_BG-2026-0452.pdf", 256));

        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentBsf, draft.DocumentFormKey);

        var bankName = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.BankName));
        Assert.Equal(string.Empty, bankName.Value);
        Assert.True(bankName.IsExpectedByDocumentForm);
        Assert.Null(bankName.ProvenanceResourceKey);
    }

    [Fact]
    public async Task ExtractAsync_detects_sabb_bank_letter_family_and_uses_family_reference_prefix()
    {
        var engine = new LocalIntakeExtractionEngine();

        var draft = await engine.ExtractAsync(
            IntakeScenarioKeys.ExtensionConfirmation,
            new StagedIntakeDocumentDto("token-sabb1", "sabb-extension-scan.jpg", 256));

        Assert.Equal(GuaranteeDocumentFormKeys.BankLetterSabb, draft.DocumentFormKey);

        var bankReference = Assert.Single(draft.Fields.Where(field => field.FieldKey == IntakeFieldKeys.BankReference));
        Assert.Equal(string.Empty, bankReference.Value);
        Assert.True(bankReference.IsExpectedByDocumentForm);
        Assert.Null(bankReference.ProvenanceResourceKey);
    }
}

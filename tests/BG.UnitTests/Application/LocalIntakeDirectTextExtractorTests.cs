using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Application.Services;

namespace BG.UnitTests.Application;

public sealed class LocalIntakeDirectTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_uses_direct_pdf_text_fields_when_worker_returns_text_route()
    {
        var extractor = new LocalIntakeDirectTextExtractor(
            new StubOcrDocumentProcessingService(
                new OcrDocumentProcessingResult(
                    true,
                    "test-worker",
                    "wave2-text-first",
                    [
                        new OcrDocumentFieldCandidateDto(
                            IntakeFieldKeys.GuaranteeNumber,
                            "BG-2026-5555",
                            99,
                            1,
                            "auto",
                            "direct-pdf-text")
                    ],
                    [],
                    null,
                    null)));

        var stagedPath = CreateTempPdfPlaceholder();

        try
        {
            var result = await extractor.ExtractAsync(
                IntakeScenarioCatalog.Find(IntakeScenarioKeys.ExtensionConfirmation)!,
                new StagedIntakeDocumentDto("token-1", "letter.pdf", 256, stagedPath),
                GuaranteeDocumentFormCatalog.Find(GuaranteeDocumentFormKeys.BankLetterGeneric)!);

            Assert.Contains(result, candidate => candidate.FieldKey == IntakeFieldKeys.GuaranteeNumber && candidate.Value == "BG-2026-5555");
        }
        finally
        {
            File.Delete(stagedPath);
        }
    }

    [Fact]
    public async Task ExtractAsync_ignores_scan_route_worker_fields_and_falls_back_to_heuristics()
    {
        var extractor = new LocalIntakeDirectTextExtractor(
            new StubOcrDocumentProcessingService(
                new OcrDocumentProcessingResult(
                    true,
                    "test-worker",
                    "wave2-scan-route",
                    [
                        new OcrDocumentFieldCandidateDto(
                            IntakeFieldKeys.GuaranteeNumber,
                            "BG-2026-7777",
                            90,
                            1,
                            "auto",
                            "ocr-layout")
                    ],
                    [],
                    null,
                    null)));

        var stagedPath = CreateTempPdfPlaceholder();

        try
        {
            var result = await extractor.ExtractAsync(
                IntakeScenarioCatalog.Find(IntakeScenarioKeys.ExtensionConfirmation)!,
                new StagedIntakeDocumentDto("token-1", "scan.pdf", 256, stagedPath),
                GuaranteeDocumentFormCatalog.Find(GuaranteeDocumentFormKeys.BankLetterGeneric)!);

            Assert.DoesNotContain(result, candidate => candidate.FieldKey == IntakeFieldKeys.GuaranteeNumber && candidate.Value == "BG-2026-7777");
            Assert.Contains(result, candidate => candidate.FieldKey == IntakeFieldKeys.BankReference);
        }
        finally
        {
            File.Delete(stagedPath);
        }
    }

    private static string CreateTempPdfPlaceholder()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bg-direct-text-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "placeholder");
        return path;
    }

    private sealed class StubOcrDocumentProcessingService : IOcrDocumentProcessingService
    {
        private readonly OcrDocumentProcessingResult _result;

        public StubOcrDocumentProcessingService(OcrDocumentProcessingResult result)
        {
            _result = result;
        }

        public Task<OcrDocumentProcessingResult> ProcessAsync(
            OcrDocumentProcessingRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }
}

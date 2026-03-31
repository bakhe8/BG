using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Application.Services;

namespace BG.UnitTests.Application;

public sealed class LocalIntakeOcrExtractorTests
{
    [Fact]
    public async Task ExtractAsync_uses_worker_fields_when_available_and_merges_fallback_fields()
    {
        var extractor = new LocalIntakeOcrExtractor(
            new StubOcrDocumentProcessingService(
                new OcrDocumentProcessingResult(
                    true,
                    "test-worker",
                    "wave1",
                    [
                        new OcrDocumentFieldCandidateDto(
                            IntakeFieldKeys.GuaranteeNumber,
                            "BG-2026-9999",
                            99,
                            1,
                            "10,10,200,40",
                            "worker")
                    ],
                    [],
                    null,
                    null)));

        var result = await extractor.ExtractAsync(
            IntakeScenarioCatalog.Find(IntakeScenarioKeys.ExtensionConfirmation)!,
            new StagedIntakeDocumentDto("token-1", "scan.jpg", 256, typeof(LocalIntakeOcrExtractorTests).Assembly.Location),
            GuaranteeDocumentFormCatalog.Find(GuaranteeDocumentFormKeys.BankLetterGeneric)!);

        Assert.Contains(result, candidate => candidate.FieldKey == IntakeFieldKeys.GuaranteeNumber && candidate.Value == "BG-2026-9999");
        Assert.Contains(result, candidate => candidate.FieldKey == IntakeFieldKeys.BankReference);
    }

    [Fact]
    public async Task ExtractAsync_falls_back_when_worker_has_no_fields()
    {
        var extractor = new LocalIntakeOcrExtractor(
            new StubOcrDocumentProcessingService(
                new OcrDocumentProcessingResult(
                    true,
                    "test-worker",
                    "wave1",
                    [],
                    [],
                    null,
                    null)));

        var result = await extractor.ExtractAsync(
            IntakeScenarioCatalog.Find(IntakeScenarioKeys.ExtensionConfirmation)!,
            new StagedIntakeDocumentDto("token-1", "scan.jpg", 256, typeof(LocalIntakeOcrExtractorTests).Assembly.Location),
            GuaranteeDocumentFormCatalog.Find(GuaranteeDocumentFormKeys.BankLetterGeneric)!);

        Assert.Contains(result, candidate => candidate.FieldKey == IntakeFieldKeys.BankReference);
        Assert.DoesNotContain(result, candidate => candidate.FieldKey == IntakeFieldKeys.GuaranteeNumber && candidate.Value == "BG-2026-9999");
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

using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Application.Contracts.Services;

namespace BG.Application.Services;

internal sealed class LocalIntakeOcrExtractor : IIntakeOcrExtractor
{
    private readonly IOcrDocumentProcessingService _ocrProcessingService;

    public LocalIntakeOcrExtractor()
        : this(new NullOcrDocumentProcessingService())
    {
    }

    internal LocalIntakeOcrExtractor(IOcrDocumentProcessingService ocrProcessingService)
    {
        _ocrProcessingService = ocrProcessingService;
    }

    public Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        GuaranteeDocumentFormDefinition documentForm,
        CancellationToken cancellationToken = default)
    {
        return ExtractInternalAsync(scenario, stagedDocument, documentForm, cancellationToken);
    }

    private async Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractInternalAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        GuaranteeDocumentFormDefinition documentForm,
        CancellationToken cancellationToken)
    {
        var fallbackCandidates = LocalIntakeExtractionHeuristics.CreateCandidates(
            scenario,
            stagedDocument,
            documentForm,
            IntakeFieldValueSource.OcrFallback);

        if (string.IsNullOrWhiteSpace(stagedDocument.StagedFilePath) || !File.Exists(stagedDocument.StagedFilePath))
        {
            return fallbackCandidates;
        }

        var ocrResult = await _ocrProcessingService.ProcessAsync(
            new OcrDocumentProcessingRequest(
                stagedDocument.Token,
                stagedDocument.StagedFilePath,
                stagedDocument.OriginalFileName,
                scenario.Key,
                documentForm.Key,
                documentForm.BankProfileKey,
                documentForm.StructuralClassKey,
                documentForm.CanonicalBankName,
                documentForm.ReferencePrefix),
            cancellationToken);

        if (!ocrResult.Succeeded || ocrResult.Fields.Count == 0)
        {
            return fallbackCandidates;
        }

        var mappedCandidates = ocrResult.Fields
            .Where(field => !string.IsNullOrWhiteSpace(field.FieldKey) && !string.IsNullOrWhiteSpace(field.Value))
            .Select(field => new IntakeExtractionFieldCandidate(field.FieldKey, field.Value, IntakeFieldValueSource.OcrFallback))
            .ToArray();

        if (mappedCandidates.Length == 0)
        {
            return fallbackCandidates;
        }

        return mappedCandidates
            .Concat(fallbackCandidates)
            .GroupBy(candidate => candidate.FieldKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }
}

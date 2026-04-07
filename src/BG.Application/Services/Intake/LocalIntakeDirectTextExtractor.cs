using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Application.Contracts.Services;

namespace BG.Application.Services;

internal sealed class LocalIntakeDirectTextExtractor : IIntakeDirectTextExtractor
{
    private readonly IOcrDocumentProcessingService _ocrProcessingService;

    public LocalIntakeDirectTextExtractor()
        : this(new NullOcrDocumentProcessingService())
    {
    }

    internal LocalIntakeDirectTextExtractor(IOcrDocumentProcessingService ocrProcessingService)
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
            IntakeFieldValueSource.DirectPdfText);

        if (!stagedDocument.OriginalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(stagedDocument.StagedFilePath) ||
            !File.Exists(stagedDocument.StagedFilePath))
        {
            return fallbackCandidates;
        }

        var processingResult = await _ocrProcessingService.ProcessAsync(
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

        if (!processingResult.Succeeded || processingResult.Fields.Count == 0)
        {
            return fallbackCandidates;
        }

        var mappedCandidates = processingResult.Fields
            .Where(field => string.Equals(field.SourceLabel, "direct-pdf-text", StringComparison.OrdinalIgnoreCase))
            .Where(field => !string.IsNullOrWhiteSpace(field.FieldKey) && !string.IsNullOrWhiteSpace(field.Value))
            .Select(field => new IntakeExtractionFieldCandidate(
                field.FieldKey,
                field.Value,
                field.Value,
                IntakeFieldValueSource.DirectPdfText,
                field.ConfidencePercent))
            .ToArray();

        if (mappedCandidates.Length == 0)
        {
            return fallbackCandidates;
        }

        return mappedCandidates
            .Concat(fallbackCandidates)
            .GroupBy(candidate => candidate.FieldKey, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(candidate => IntakeFieldProvenanceCatalog.GetPriority(candidate.Source))
                .ThenByDescending(candidate => candidate.ConfidencePercent)
                .First())
            .ToArray();
    }
}

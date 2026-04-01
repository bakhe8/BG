using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Services;

internal sealed class LocalIntakeExtractionEngine : IIntakeExtractionEngine
{
    private readonly IIntakeDocumentClassifier _documentClassifier;
    private readonly IIntakeDirectTextExtractor _directTextExtractor;
    private readonly IIntakeOcrExtractor _ocrExtractor;
    private readonly IIntakeFieldReviewProjector _fieldReviewProjector;

    public LocalIntakeExtractionEngine()
        : this(
            new LocalIntakeDocumentClassifier(),
            new LocalIntakeDirectTextExtractor(),
            new LocalIntakeOcrExtractor(),
            new LocalIntakeFieldReviewProjector(new LocalIntakeExtractionConfidenceScorer()))
    {
    }

    internal LocalIntakeExtractionEngine(
        IIntakeDocumentClassifier documentClassifier,
        IIntakeDirectTextExtractor directTextExtractor,
        IIntakeOcrExtractor ocrExtractor,
        IIntakeFieldReviewProjector fieldReviewProjector)
    {
        _documentClassifier = documentClassifier;
        _directTextExtractor = directTextExtractor;
        _ocrExtractor = ocrExtractor;
        _fieldReviewProjector = fieldReviewProjector;
    }

    public async Task<IntakeExtractionDraftDto> ExtractAsync(
        string scenarioKey,
        StagedIntakeDocumentDto stagedDocument,
        CancellationToken cancellationToken = default)
    {
        var scenario = IntakeScenarioCatalog.Find(scenarioKey)
            ?? throw new InvalidOperationException($"Unsupported intake scenario '{scenarioKey}'.");

        var classification = _documentClassifier.Classify(stagedDocument);
        var detectedForm = GuaranteeDocumentFormCatalog.ResolveDetectedForm(scenario.Key, stagedDocument.OriginalFileName);
        var candidates = await ExtractCandidatesAsync(scenario, stagedDocument, classification, detectedForm, cancellationToken);
        detectedForm = GuaranteeDocumentFormCatalog.ResolveDetectedForm(scenario.Key, stagedDocument.OriginalFileName, candidates);
        var fields = _fieldReviewProjector.Project(scenario, detectedForm, candidates);

        return new IntakeExtractionDraftDto(
            scenario.Key,
            stagedDocument.Token,
            stagedDocument.OriginalFileName,
            classification.PageCount,
            classification.RouteResourceKey,
            fields,
            detectedForm.Key);
    }

    private async Task<IReadOnlyList<IntakeExtractionFieldCandidate>> ExtractCandidatesAsync(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        IntakeDocumentClassificationResult classification,
        GuaranteeDocumentFormDefinition detectedForm,
        CancellationToken cancellationToken)
    {
        if (classification.Strategy == IntakeExtractionStrategy.OcrOnly)
        {
            return await _ocrExtractor.ExtractAsync(scenario, stagedDocument, detectedForm, cancellationToken);
        }

        var directCandidates = await _directTextExtractor.ExtractAsync(scenario, stagedDocument, detectedForm, cancellationToken);
        if (!NeedsOcrSupplement(scenario, detectedForm, directCandidates))
        {
            return directCandidates;
        }

        var ocrCandidates = await _ocrExtractor.ExtractAsync(scenario, stagedDocument, detectedForm, cancellationToken);
        return directCandidates
            .Concat(ocrCandidates)
            .ToArray();
    }

    private static bool NeedsOcrSupplement(
        IntakeScenarioDefinition scenario,
        GuaranteeDocumentFormDefinition documentForm,
        IReadOnlyList<IntakeExtractionFieldCandidate> directCandidates)
    {
        var extractedKeys = directCandidates
            .Select(candidate => candidate.FieldKey)
            .ToHashSet(StringComparer.Ordinal);

        return scenario.RequiredReviewFieldKeys
            .Where(fieldKey => documentForm.ExpectedFieldKeys.Contains(fieldKey, StringComparer.Ordinal))
            .Any(fieldKey => !extractedKeys.Contains(fieldKey));
    }
}

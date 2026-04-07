using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using System.Globalization;

namespace BG.Application.Services;

internal sealed class LocalIntakeExtractionEngine : IIntakeExtractionEngine
{
    private readonly IIntakeDocumentClassifier _documentClassifier;
    private readonly IIntakeDirectTextExtractor _directTextExtractor;
    private readonly IIntakeOcrExtractor _ocrExtractor;
    private readonly IIntakeCandidateValidator _candidateValidator;
    private readonly IIntakeFieldReviewProjector _fieldReviewProjector;

    public LocalIntakeExtractionEngine()
        : this(
            new LocalIntakeDocumentClassifier(),
            new LocalIntakeDirectTextExtractor(),
            new LocalIntakeOcrExtractor(),
            new LocalIntakeCandidateValidator(),
            new LocalIntakeFieldReviewProjector(new LocalIntakeExtractionConfidenceScorer()))
    {
    }

    internal LocalIntakeExtractionEngine(
        IIntakeDocumentClassifier documentClassifier,
        IIntakeDirectTextExtractor directTextExtractor,
        IIntakeOcrExtractor ocrExtractor,
        IIntakeCandidateValidator candidateValidator,
        IIntakeFieldReviewProjector fieldReviewProjector)
    {
        _documentClassifier = documentClassifier;
        _directTextExtractor = directTextExtractor;
        _ocrExtractor = ocrExtractor;
        _candidateValidator = candidateValidator;
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
        var rawCandidates = await ExtractCandidatesAsync(scenario, stagedDocument, classification, detectedForm, cancellationToken);
        var candidates = _candidateValidator.Validate(rawCandidates);
        detectedForm = GuaranteeDocumentFormCatalog.ResolveDetectedForm(scenario.Key, stagedDocument.OriginalFileName, candidates);
        var projectedFields = _fieldReviewProjector.Project(scenario, detectedForm, candidates);
        var fields = ApplyPostProjectionCrossFieldValidation(projectedFields);

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

    private static IReadOnlyList<IntakeFieldReviewDto> ApplyPostProjectionCrossFieldValidation(
        IReadOnlyList<IntakeFieldReviewDto> fields)
    {
        var updatedByKey = new Dictionary<string, IntakeFieldReviewDto>(StringComparer.Ordinal);

        var issueDate = GetDate(fields, IntakeFieldKeys.IssueDate);
        var expiryDate = GetDate(fields, IntakeFieldKeys.ExpiryDate);
        if (issueDate.HasValue && expiryDate.HasValue && expiryDate.Value <= issueDate.Value)
        {
            var existing = fields.FirstOrDefault(field => field.FieldKey == IntakeFieldKeys.ExpiryDate);
            if (existing is not null)
            {
                updatedByKey[IntakeFieldKeys.ExpiryDate] = existing with
                {
                    RequiresExplicitReview = true,
                    ReviewReason = MergeReviewReason(existing.ReviewReason, "expiry-not-after-issue")
                };
            }
        }

        var newExpiryDate = GetDate(fields, IntakeFieldKeys.NewExpiryDate);
        if (expiryDate.HasValue && newExpiryDate.HasValue && newExpiryDate.Value <= expiryDate.Value)
        {
            var existing = fields.FirstOrDefault(field => field.FieldKey == IntakeFieldKeys.NewExpiryDate);
            if (existing is not null)
            {
                updatedByKey[IntakeFieldKeys.NewExpiryDate] = existing with
                {
                    RequiresExplicitReview = true,
                    ReviewReason = MergeReviewReason(existing.ReviewReason, "new-expiry-not-after-current-expiry")
                };
            }
        }

        if (updatedByKey.Count == 0)
            return fields;

        return fields
            .Select(field => updatedByKey.TryGetValue(field.FieldKey, out var updated) ? updated : field)
            .ToArray();
    }

    private static DateOnly? GetDate(IReadOnlyList<IntakeFieldReviewDto> fields, string fieldKey)
    {
        var value = fields.FirstOrDefault(field => field.FieldKey == fieldKey)?.Value;
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(
            value,
            ["yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static string MergeReviewReason(string? existing, string added)
    {
        if (string.IsNullOrWhiteSpace(existing))
            return added;

        return existing.Contains(added, StringComparison.Ordinal)
            ? existing
            : $"{existing};{added}";
    }
}

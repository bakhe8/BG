using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Services;

internal sealed class LocalIntakeFieldReviewProjector : IIntakeFieldReviewProjector
{
    private const int ConfidenceThreshold = 40;
    private readonly IIntakeExtractionConfidenceScorer _confidenceScorer;

    public LocalIntakeFieldReviewProjector(IIntakeExtractionConfidenceScorer confidenceScorer)
    {
        _confidenceScorer = confidenceScorer;
    }

    public IReadOnlyList<IntakeFieldReviewDto> Project(
        IntakeScenarioDefinition scenario,
        GuaranteeDocumentFormDefinition documentForm,
        IEnumerable<IntakeExtractionFieldCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(documentForm);
        ArgumentNullException.ThrowIfNull(candidates);

        var expectedFieldKeys = documentForm.ExpectedFieldKeys.ToHashSet(StringComparer.Ordinal);

        var candidateGroups = candidates
            .GroupBy(candidate => candidate.FieldKey, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(candidate => IntakeFieldProvenanceCatalog.GetPriority(candidate.Source))
                    .ThenByDescending(candidate => candidate.ConfidencePercent)
                    .ToArray(),
                StringComparer.Ordinal);

        return scenario.SampleFields
            .Select(sampleField =>
            {
                if (candidateGroups.TryGetValue(sampleField.FieldKey, out var fieldCandidates))
                {
                    var best = fieldCandidates[0];
                    var confidence = _confidenceScorer.Score(sampleField, best);
                    var sourcesAgreed = ComputeSourcesAgreed(fieldCandidates);
                    var conflictDetail = sourcesAgreed ? null : BuildConflictDetail(fieldCandidates);
                    var requiresReview = confidence < ConfidenceThreshold || !sourcesAgreed;

                    return sampleField with
                    {
                        Value = best.Value,
                        RawValue = best.RawValue,
                        ConfidencePercent = confidence,
                        RequiresExplicitReview = requiresReview,
                        ProvenanceResourceKey = IntakeFieldProvenanceCatalog.GetResourceKey(best.Source),
                        IsExpectedByDocumentForm = expectedFieldKeys.Contains(sampleField.FieldKey),
                        SourcesAgreed = sourcesAgreed,
                        SourcesConflictDetail = conflictDetail,
                        ReviewReason = requiresReview
                            ? (confidence < ConfidenceThreshold ? "low-confidence" : "sources-conflict")
                            : null
                    };
                }

                return sampleField with
                {
                    Value = string.Empty,
                    RawValue = null,
                    ConfidencePercent = 0,
                    RequiresExplicitReview = true,
                    ProvenanceResourceKey = null,
                    IsExpectedByDocumentForm = expectedFieldKeys.Contains(sampleField.FieldKey),
                    SourcesAgreed = false,
                    SourcesConflictDetail = null,
                    ReviewReason = "not-extracted"
                };
            })
            .ToArray();
    }

    private static bool ComputeSourcesAgreed(IntakeExtractionFieldCandidate[] candidates)
    {
        if (candidates.Length <= 1)
            return true;

        var distinctSources = candidates.Select(c => c.Source).Distinct().Count();
        if (distinctSources <= 1)
            return true;

        var distinctValues = candidates
            .Select(c => c.Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return distinctValues == 1;
    }

    private static string BuildConflictDetail(IntakeExtractionFieldCandidate[] candidates)
    {
        var parts = candidates.Take(3).Select(c => $"{c.Source}={c.Value}");
        return string.Join("; ", parts);
    }
}

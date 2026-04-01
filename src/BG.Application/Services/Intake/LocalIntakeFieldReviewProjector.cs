using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Services;

internal sealed class LocalIntakeFieldReviewProjector : IIntakeFieldReviewProjector
{
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

        var candidateMap = candidates
            .GroupBy(candidate => candidate.FieldKey, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(candidate => IntakeFieldProvenanceCatalog.GetPriority(candidate.Source))
                    .First(),
                StringComparer.Ordinal);

        return scenario.SampleFields
            .Select(sampleField =>
            {
                if (candidateMap.TryGetValue(sampleField.FieldKey, out var candidate))
                {
                    return sampleField with
                    {
                        Value = candidate.Value,
                        ConfidencePercent = _confidenceScorer.Score(sampleField, candidate.Source),
                        ProvenanceResourceKey = IntakeFieldProvenanceCatalog.GetResourceKey(candidate.Source),
                        IsExpectedByDocumentForm = expectedFieldKeys.Contains(sampleField.FieldKey)
                    };
                }

                return sampleField with
                {
                    ConfidencePercent = _confidenceScorer.Score(sampleField, IntakeFieldValueSource.ScenarioSample),
                    ProvenanceResourceKey = IntakeFieldProvenanceCatalog.GetResourceKey(IntakeFieldValueSource.ScenarioSample),
                    IsExpectedByDocumentForm = expectedFieldKeys.Contains(sampleField.FieldKey)
                };
            })
            .ToArray();
    }
}

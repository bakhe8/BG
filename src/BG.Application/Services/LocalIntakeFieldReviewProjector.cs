using BG.Application.Intake;
using BG.Application.Models.Intake;

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
        IEnumerable<IntakeExtractionFieldCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(candidates);

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
                        ProvenanceResourceKey = IntakeFieldProvenanceCatalog.GetResourceKey(candidate.Source)
                    };
                }

                return sampleField with
                {
                    ConfidencePercent = _confidenceScorer.Score(sampleField, IntakeFieldValueSource.ScenarioSample),
                    ProvenanceResourceKey = IntakeFieldProvenanceCatalog.GetResourceKey(IntakeFieldValueSource.ScenarioSample)
                };
            })
            .ToArray();
    }
}

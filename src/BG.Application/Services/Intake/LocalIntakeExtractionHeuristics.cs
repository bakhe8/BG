using System.Text.RegularExpressions;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;

namespace BG.Application.Services;

internal static partial class LocalIntakeExtractionHeuristics
{
    public static IReadOnlyList<IntakeExtractionFieldCandidate> CreateCandidates(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        GuaranteeDocumentFormDefinition documentForm,
        IntakeFieldValueSource defaultSource)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(stagedDocument);
        ArgumentNullException.ThrowIfNull(documentForm);

        var extractedGuaranteeNumber = ExtractGuaranteeNumber(stagedDocument.OriginalFileName);
        if (!string.IsNullOrWhiteSpace(extractedGuaranteeNumber))
        {
            return
            [
                new IntakeExtractionFieldCandidate(
                    IntakeFieldKeys.GuaranteeNumber,
                    extractedGuaranteeNumber,
                    extractedGuaranteeNumber,
                    IntakeFieldValueSource.FilenamePattern,
                    55)
            ];
        }

        return [];
    }

    private static string? ExtractGuaranteeNumber(string originalFileName)
    {
        var match = GuaranteeNumberRegex().Match(Path.GetFileNameWithoutExtension(originalFileName));
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    [GeneratedRegex("BG-\\d{4}-\\d{3,6}", RegexOptions.IgnoreCase)]
    private static partial Regex GuaranteeNumberRegex();
}

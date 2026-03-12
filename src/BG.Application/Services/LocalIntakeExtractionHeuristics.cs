using System.Text.RegularExpressions;
using BG.Application.Intake;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal static partial class LocalIntakeExtractionHeuristics
{
    public static IReadOnlyList<IntakeExtractionFieldCandidate> CreateCandidates(
        IntakeScenarioDefinition scenario,
        StagedIntakeDocumentDto stagedDocument,
        IntakeFieldValueSource defaultSource)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(stagedDocument);

        var extractedGuaranteeNumber = ExtractGuaranteeNumber(stagedDocument.OriginalFileName);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return scenario.SampleFields
            .Select(field =>
            {
                var value = ResolveFieldValue(scenario, field.FieldKey, field.Value, today, stagedDocument.Token, extractedGuaranteeNumber);
                var source = field.FieldKey == IntakeFieldKeys.GuaranteeNumber && !string.IsNullOrWhiteSpace(extractedGuaranteeNumber)
                    ? IntakeFieldValueSource.FilenamePattern
                    : defaultSource;

                return new IntakeExtractionFieldCandidate(field.FieldKey, value, source);
            })
            .ToArray();
    }

    private static string ResolveFieldValue(
        IntakeScenarioDefinition scenario,
        string fieldKey,
        string fallbackValue,
        DateOnly today,
        string stagedDocumentToken,
        string? extractedGuaranteeNumber)
    {
        if (!string.IsNullOrWhiteSpace(extractedGuaranteeNumber) && fieldKey == IntakeFieldKeys.GuaranteeNumber)
        {
            return extractedGuaranteeNumber;
        }

        var tokenSuffix = stagedDocumentToken[..4].ToUpperInvariant();

        return (scenario.Kind, fieldKey) switch
        {
            (IntakeScenarioKind.NewGuarantee, IntakeFieldKeys.GuaranteeNumber) => $"BG-{today:yyyy}-{tokenSuffix}",
            (IntakeScenarioKind.NewGuarantee, IntakeFieldKeys.IssueDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.NewGuarantee, IntakeFieldKeys.ExpiryDate) => today.AddYears(1).ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ExtensionConfirmation, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ExtensionConfirmation, IntakeFieldKeys.NewExpiryDate) => today.AddYears(1).AddMonths(6).ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ReductionConfirmation, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ReductionConfirmation, IntakeFieldKeys.Amount) => "950000",
            (IntakeScenarioKind.ReleaseConfirmation, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.StatusVerification, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            _ => fallbackValue
        };
    }

    private static string? ExtractGuaranteeNumber(string originalFileName)
    {
        var match = GuaranteeNumberRegex().Match(Path.GetFileNameWithoutExtension(originalFileName));
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    [GeneratedRegex("BG-\\d{4}-\\d{3,6}", RegexOptions.IgnoreCase)]
    private static partial Regex GuaranteeNumberRegex();
}

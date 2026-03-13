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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedFieldKeys = documentForm.ExpectedFieldKeys.ToHashSet(StringComparer.Ordinal);

        return scenario.SampleFields
            .Where(
                field => expectedFieldKeys.Contains(field.FieldKey) ||
                         (field.FieldKey == IntakeFieldKeys.GuaranteeNumber && !string.IsNullOrWhiteSpace(extractedGuaranteeNumber)))
            .Select(field =>
            {
                var value = ResolveFieldValue(
                    scenario,
                    documentForm,
                    field.FieldKey,
                    field.Value,
                    today,
                    stagedDocument.Token,
                    extractedGuaranteeNumber);
                var source = field.FieldKey == IntakeFieldKeys.GuaranteeNumber && !string.IsNullOrWhiteSpace(extractedGuaranteeNumber)
                    ? IntakeFieldValueSource.FilenamePattern
                    : defaultSource;

                return new IntakeExtractionFieldCandidate(field.FieldKey, value, source);
            })
            .ToArray();
    }

    private static string ResolveFieldValue(
        IntakeScenarioDefinition scenario,
        GuaranteeDocumentFormDefinition documentForm,
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
            (IntakeScenarioKind.NewGuarantee, IntakeFieldKeys.BankName) when !string.IsNullOrWhiteSpace(documentForm.CanonicalBankName) => documentForm.CanonicalBankName!,
            (IntakeScenarioKind.NewGuarantee, IntakeFieldKeys.IssueDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.NewGuarantee, IntakeFieldKeys.ExpiryDate) => today.AddYears(1).ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ExtensionConfirmation, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ExtensionConfirmation, IntakeFieldKeys.NewExpiryDate) => today.AddYears(1).AddMonths(6).ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ExtensionConfirmation, IntakeFieldKeys.BankReference) => ResolveBankReference(documentForm, "EXT-44791"),
            (IntakeScenarioKind.ReductionConfirmation, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ReductionConfirmation, IntakeFieldKeys.Amount) => "950000",
            (IntakeScenarioKind.ReductionConfirmation, IntakeFieldKeys.BankReference) => ResolveBankReference(documentForm, "RED-55122"),
            (IntakeScenarioKind.ReleaseConfirmation, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.ReleaseConfirmation, IntakeFieldKeys.BankReference) => ResolveBankReference(documentForm, "REL-13210"),
            (IntakeScenarioKind.StatusVerification, IntakeFieldKeys.OfficialLetterDate) => today.ToString("yyyy-MM-dd"),
            (IntakeScenarioKind.StatusVerification, IntakeFieldKeys.BankReference) => ResolveBankReference(documentForm, "STAT-8810"),
            (IntakeScenarioKind.StatusVerification, IntakeFieldKeys.StatusStatement) when !string.IsNullOrWhiteSpace(documentForm.CanonicalBankName)
                => $"{documentForm.CanonicalBankName} confirms the guarantee remains active without change.",
            _ => fallbackValue
        };
    }

    private static string ResolveBankReference(GuaranteeDocumentFormDefinition documentForm, string fallbackValue)
    {
        return documentForm.Key switch
        {
            GuaranteeDocumentFormKeys.BankLetterSnb => $"SNB-{fallbackValue}",
            GuaranteeDocumentFormKeys.BankLetterAlRajhi => $"RJH-{fallbackValue}",
            GuaranteeDocumentFormKeys.BankLetterRiyad => $"RYD-{fallbackValue}",
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

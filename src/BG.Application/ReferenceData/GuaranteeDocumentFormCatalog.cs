using System.IO;
using BG.Application.Intake;
using BG.Application.Models.Documents;
using BG.Application.Models.Intake;
using BG.Domain.Guarantees;

namespace BG.Application.ReferenceData;

public static class GuaranteeDocumentFormCatalog
{
    private static readonly IReadOnlyList<GuaranteeDocumentFormDefinition> Forms =
    [
        new(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric,
            GuaranteeDocumentType.GuaranteeInstrument,
            "BankProfile_Generic",
            "DocumentForm_Instrument_Generic_Title",
            "DocumentForm_Instrument_Generic_Summary",
            CanonicalBankName: null,
            [IntakeScenarioKeys.NewGuarantee],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.CurrencyCode,
                IntakeFieldKeys.IssueDate,
                IntakeFieldKeys.ExpiryDate
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_GuaranteeReference",
                "DocumentFormCue_AmountCurrency",
                "DocumentFormCue_IssueExpiry"
            ],
            [],
            []),
        new(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
            GuaranteeDocumentType.GuaranteeInstrument,
            "BankProfile_SNB",
            "DocumentForm_Instrument_SNB_Title",
            "DocumentForm_Instrument_SNB_Summary",
            "Saudi National Bank",
            [IntakeScenarioKeys.NewGuarantee],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.BankName,
                IntakeFieldKeys.Beneficiary,
                IntakeFieldKeys.Principal,
                IntakeFieldKeys.GuaranteeCategory,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.CurrencyCode,
                IntakeFieldKeys.IssueDate,
                IntakeFieldKeys.ExpiryDate
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_GuaranteeReference",
                "DocumentFormCue_AmountCurrency",
                "DocumentFormCue_IssueExpiry"
            ],
            ["snb", "saudi-national-bank", "saudi_national_bank", "alahli"],
            ["saudi national bank", "snb", "alahli", "al ahli"]),
        new(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentAlRajhi,
            GuaranteeDocumentType.GuaranteeInstrument,
            "BankProfile_AlRajhi",
            "DocumentForm_Instrument_AlRajhi_Title",
            "DocumentForm_Instrument_AlRajhi_Summary",
            "Al Rajhi Bank",
            [IntakeScenarioKeys.NewGuarantee],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.BankName,
                IntakeFieldKeys.Beneficiary,
                IntakeFieldKeys.Principal,
                IntakeFieldKeys.GuaranteeCategory,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.CurrencyCode,
                IntakeFieldKeys.IssueDate,
                IntakeFieldKeys.ExpiryDate
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_GuaranteeReference",
                "DocumentFormCue_AmountCurrency",
                "DocumentFormCue_IssueExpiry"
            ],
            ["alrajhi", "al-rajhi", "rajhi"],
            ["al rajhi bank", "alrajhi", "rajhi"]),
        new(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentRiyad,
            GuaranteeDocumentType.GuaranteeInstrument,
            "BankProfile_Riyad",
            "DocumentForm_Instrument_Riyad_Title",
            "DocumentForm_Instrument_Riyad_Summary",
            "Riyad Bank",
            [IntakeScenarioKeys.NewGuarantee],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.BankName,
                IntakeFieldKeys.Beneficiary,
                IntakeFieldKeys.Principal,
                IntakeFieldKeys.GuaranteeCategory,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.CurrencyCode,
                IntakeFieldKeys.IssueDate,
                IntakeFieldKeys.ExpiryDate
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_GuaranteeReference",
                "DocumentFormCue_AmountCurrency",
                "DocumentFormCue_IssueExpiry"
            ],
            ["riyad", "riyadbank"],
            ["riyad bank", "riyadbank", "riyad"]),
        new(
            GuaranteeDocumentFormKeys.BankLetterGeneric,
            GuaranteeDocumentType.BankResponse,
            "BankProfile_Generic",
            "DocumentForm_BankLetter_Generic_Title",
            "DocumentForm_BankLetter_Generic_Summary",
            CanonicalBankName: null,
            [
                IntakeScenarioKeys.ExtensionConfirmation,
                IntakeScenarioKeys.ReductionConfirmation,
                IntakeScenarioKeys.ReleaseConfirmation,
                IntakeScenarioKeys.StatusVerification
            ],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.BankReference
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_OfficialLetterDate",
                "DocumentFormCue_BankReference"
            ],
            [],
            []),
        new(
            GuaranteeDocumentFormKeys.BankLetterSnb,
            GuaranteeDocumentType.BankResponse,
            "BankProfile_SNB",
            "DocumentForm_BankLetter_SNB_Title",
            "DocumentForm_BankLetter_SNB_Summary",
            "Saudi National Bank",
            [
                IntakeScenarioKeys.ExtensionConfirmation,
                IntakeScenarioKeys.ReductionConfirmation,
                IntakeScenarioKeys.ReleaseConfirmation,
                IntakeScenarioKeys.StatusVerification
            ],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.BankReference,
                IntakeFieldKeys.NewExpiryDate,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.StatusStatement
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_OfficialLetterDate",
                "DocumentFormCue_BankReference"
            ],
            ["snb", "saudi-national-bank", "saudi_national_bank", "alahli"],
            ["saudi national bank", "snb", "alahli", "al ahli"]),
        new(
            GuaranteeDocumentFormKeys.BankLetterAlRajhi,
            GuaranteeDocumentType.BankResponse,
            "BankProfile_AlRajhi",
            "DocumentForm_BankLetter_AlRajhi_Title",
            "DocumentForm_BankLetter_AlRajhi_Summary",
            "Al Rajhi Bank",
            [
                IntakeScenarioKeys.ExtensionConfirmation,
                IntakeScenarioKeys.ReductionConfirmation,
                IntakeScenarioKeys.ReleaseConfirmation,
                IntakeScenarioKeys.StatusVerification
            ],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.BankReference,
                IntakeFieldKeys.NewExpiryDate,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.StatusStatement
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_OfficialLetterDate",
                "DocumentFormCue_BankReference"
            ],
            ["alrajhi", "al-rajhi", "rajhi"],
            ["al rajhi bank", "alrajhi", "rajhi"]),
        new(
            GuaranteeDocumentFormKeys.BankLetterRiyad,
            GuaranteeDocumentType.BankResponse,
            "BankProfile_Riyad",
            "DocumentForm_BankLetter_Riyad_Title",
            "DocumentForm_BankLetter_Riyad_Summary",
            "Riyad Bank",
            [
                IntakeScenarioKeys.ExtensionConfirmation,
                IntakeScenarioKeys.ReductionConfirmation,
                IntakeScenarioKeys.ReleaseConfirmation,
                IntakeScenarioKeys.StatusVerification
            ],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.BankReference,
                IntakeFieldKeys.NewExpiryDate,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.StatusStatement
            ],
            [
                "DocumentFormCue_BankIdentity",
                "DocumentFormCue_OfficialLetterDate",
                "DocumentFormCue_BankReference"
            ],
            ["riyad", "riyadbank"],
            ["riyad bank", "riyadbank", "riyad"]),
        new(
            GuaranteeDocumentFormKeys.SupportingAttachmentGeneric,
            GuaranteeDocumentType.SupportingDocument,
            "BankProfile_Generic",
            "DocumentForm_Attachment_Generic_Title",
            "DocumentForm_Attachment_Generic_Summary",
            CanonicalBankName: null,
            [IntakeScenarioKeys.SupportingAttachment],
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.AttachmentNote
            ],
            [
                "DocumentFormCue_GuaranteeReference",
                "DocumentFormCue_AttachmentPurpose"
            ],
            [],
            [])
    ];

    public static IReadOnlyList<GuaranteeDocumentFormDefinition> GetSupportedForms(string scenarioKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioKey);

        return Forms
            .Where(form => form.ScenarioKeys.Contains(scenarioKey, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    public static GuaranteeDocumentFormDefinition GetDefaultForm(string scenarioKey)
    {
        return GetSupportedForms(scenarioKey).First();
    }

    public static GuaranteeDocumentFormDefinition? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return Forms.FirstOrDefault(form => string.Equals(form.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsSupportedForScenario(string scenarioKey, string? formKey)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            return false;
        }

        return GetSupportedForms(scenarioKey).Any(form => string.Equals(form.Key, formKey, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsScenarioSupported(string scenarioKey)
    {
        return Forms.Any(form => form.ScenarioKeys.Contains(scenarioKey, StringComparer.OrdinalIgnoreCase));
    }

    public static GuaranteeDocumentFormSnapshotDto? ToSnapshot(GuaranteeDocumentFormDefinition? form)
    {
        return form is null
            ? null
            : new GuaranteeDocumentFormSnapshotDto(
                form.Key,
                form.BankResourceKey,
                form.TitleResourceKey,
                form.SummaryResourceKey);
    }

    public static bool IsSpecificBankProfile(GuaranteeDocumentFormDefinition? form)
    {
        return form is not null &&
               !string.Equals(form.BankResourceKey, "BankProfile_Generic", StringComparison.Ordinal);
    }

    public static bool HasConflictingSpecificBankProfiles(
        GuaranteeDocumentFormDefinition? left,
        GuaranteeDocumentFormDefinition? right)
    {
        return IsSpecificBankProfile(left) &&
               IsSpecificBankProfile(right) &&
               !string.Equals(left!.BankResourceKey, right!.BankResourceKey, StringComparison.Ordinal);
    }

    internal static GuaranteeDocumentFormDefinition? GetFallbackForm(GuaranteeDocumentType documentType)
    {
        var key = documentType switch
        {
            GuaranteeDocumentType.GuaranteeInstrument => GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric,
            GuaranteeDocumentType.BankResponse => GuaranteeDocumentFormKeys.BankLetterGeneric,
            GuaranteeDocumentType.SupportingDocument => GuaranteeDocumentFormKeys.SupportingAttachmentGeneric,
            _ => null
        };

        return key is null ? null : Find(key);
    }

    internal static GuaranteeDocumentFormDefinition ResolveDetectedForm(
        string scenarioKey,
        string originalFileName,
        IEnumerable<IntakeExtractionFieldCandidate>? candidates = null)
    {
        var supportedForms = GetSupportedForms(scenarioKey);
        var defaultForm = supportedForms[0];
        var fileName = Path.GetFileNameWithoutExtension(originalFileName ?? string.Empty);
        var normalizedFileName = fileName.Trim().ToLowerInvariant();
        var signalText = BuildSignalText(normalizedFileName, candidates);

        GuaranteeDocumentFormDefinition? bestMatch = null;
        var bestScore = 0;

        foreach (var form in supportedForms)
        {
            var score = 0;

            if (ContainsAny(normalizedFileName, form.FileNameHints))
            {
                score += 8;
            }

            if (ContainsAny(signalText, form.BankNameHints))
            {
                score += 5;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = form;
            }
        }

        return bestScore > 0 && bestMatch is not null
            ? bestMatch
            : defaultForm;
    }

    private static string BuildSignalText(
        string normalizedFileName,
        IEnumerable<IntakeExtractionFieldCandidate>? candidates)
    {
        if (candidates is null)
        {
            return normalizedFileName;
        }

        var fragments = candidates
            .Select(candidate => candidate.Value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.ToLowerInvariant());

        return string.Join(' ', new[] { normalizedFileName }.Concat(fragments));
    }

    private static bool ContainsAny(string haystack, IReadOnlyList<string> needles)
    {
        if (string.IsNullOrWhiteSpace(haystack) || needles.Count == 0)
        {
            return false;
        }

        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

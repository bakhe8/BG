using System.IO;
using BG.Application.Intake;
using BG.Application.Models.Documents;
using BG.Application.Models.Intake;
using BG.Domain.Guarantees;

namespace BG.Application.ReferenceData;

public static class GuaranteeDocumentFormCatalog
{
    private static readonly GuaranteeDocumentBankProfileDefinition GenericProfile =
        new(
            GuaranteeDocumentBankProfileKeys.Generic,
            "BankProfile_Generic",
            string.Empty,
            string.Empty,
            [],
            []);

    private static readonly IReadOnlyList<GuaranteeDocumentBankProfileDefinition> SpecificProfiles =
    [
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Snb,
            "BankProfile_SNB",
            "Saudi National Bank",
            "SNB",
            ["snb", "saudi-national-bank", "saudi_national_bank", "alahli", "ahli", "الأهلي", "اهلي"],
            ["saudi national bank", "snb", "alahli", "al ahli", "البنك الأهلي السعودي", "الأهلي", "الاهلي"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.AlRajhi,
            "BankProfile_AlRajhi",
            "Al Rajhi Bank",
            "RJH",
            ["alrajhi", "al-rajhi", "rajhi", "الراجحي"],
            ["al rajhi bank", "alrajhi", "rajhi", "مصرف الراجحي", "الراجحي"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Alinma,
            "BankProfile_Alinma",
            "Alinma Bank",
            "ALN",
            ["alinma", "alinma-bank", "alinmabank", "الإنماء", "الانماء"],
            ["alinma bank", "alinma", "مصرف الإنماء", "بنك الإنماء", "الإنماء", "الانماء"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Riyad,
            "BankProfile_Riyad",
            "Riyad Bank",
            "RYD",
            ["riyad", "riyadbank", "riyadh", "الرياض"],
            ["riyad bank", "riyadbank", "riyad", "بنك الرياض", "الرياض"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.BnpParibas,
            "BankProfile_BnpParibas",
            "BNP Paribas",
            "BNP",
            ["bnp", "paribas", "bnp-paribas", "bnpparibas", "باريبا"],
            ["bnp paribas", "bnp", "paribas", "بي ان بي باريبا", "بي ان بي", "باريبا"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Anb,
            "BankProfile_ANB",
            "Arab National Bank",
            "ANB",
            ["anb", "arab-national-bank", "arabnationalbank", "العربي", "الوطني"],
            ["arab national bank", "anb", "بنك العربي الوطني", "العربي الوطني"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Bsf,
            "BankProfile_BSF",
            "Banque Saudi Fransi",
            "BSF",
            ["bsf", "fransi", "saudi-fransi", "saudifransi", "الفرنسي"],
            ["banque saudi fransi", "bsf", "saudi fransi", "البنك السعودي الفرنسي", "السعودي الفرنسي", "الفرنسي"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Sabb,
            "BankProfile_SABB",
            "SABB",
            "SABB",
            ["sabb", "saudi-british", "saudibritish", "hsbc", "ساب"],
            ["sabb", "the saudi british bank", "saudi british bank", "البنك السعودي البريطاني", "البريطاني السعودي", "ساب"]),
        CreateProfile(
            GuaranteeDocumentBankProfileKeys.Saib,
            "BankProfile_Saib",
            "Saudi Investment Bank",
            "SAIB",
            ["saib", "saudi-investment", "saudiinvestment", "investment-bank", "الاستثمار"],
            ["saudi investment bank", "saib", "البنك السعودي للاستثمار", "السعودي للاستثمار", "الاستثمار"])
    ];

    private static readonly IReadOnlyList<GuaranteeDocumentFormDefinition> Forms = BuildForms();

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
               !string.Equals(form.BankProfileKey, GuaranteeDocumentBankProfileKeys.Generic, StringComparison.Ordinal);
    }

    public static bool HasConflictingSpecificBankProfiles(
        GuaranteeDocumentFormDefinition? left,
        GuaranteeDocumentFormDefinition? right)
    {
        return IsSpecificBankProfile(left) &&
               IsSpecificBankProfile(right) &&
               !string.Equals(left!.BankProfileKey, right!.BankProfileKey, StringComparison.Ordinal);
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

    private static IReadOnlyList<GuaranteeDocumentFormDefinition> BuildForms()
    {
        var forms = new List<GuaranteeDocumentFormDefinition>
        {
            CreateGenericInstrumentForm(),
            CreateGenericBankLetterForm(),
            CreateSupportingAttachmentForm()
        };

        forms.AddRange(SpecificProfiles.Select(CreateInstrumentForm));
        forms.AddRange(SpecificProfiles.Select(CreateBankLetterForm));

        return forms;
    }

    private static GuaranteeDocumentBankProfileDefinition CreateProfile(
        string key,
        string bankResourceKey,
        string canonicalBankName,
        string referencePrefix,
        IReadOnlyList<string> fileNameHints,
        IReadOnlyList<string> bankNameHints)
    {
        return new GuaranteeDocumentBankProfileDefinition(
            key,
            bankResourceKey,
            canonicalBankName,
            referencePrefix,
            fileNameHints,
            bankNameHints);
    }

    private static GuaranteeDocumentFormDefinition CreateGenericInstrumentForm()
    {
        return new GuaranteeDocumentFormDefinition(
            GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric,
            GuaranteeDocumentType.GuaranteeInstrument,
            GenericProfile.Key,
            GenericProfile.BankResourceKey,
            "DocumentForm_Instrument_Generic_Title",
            "DocumentForm_Instrument_Generic_Summary",
            GuaranteeDocumentFormStructuralClassKeys.OriginalInstrument,
            CanonicalBankName: null,
            ReferencePrefix: null,
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
            []);
    }

    private static GuaranteeDocumentFormDefinition CreateInstrumentForm(GuaranteeDocumentBankProfileDefinition profile)
    {
        return new GuaranteeDocumentFormDefinition(
            GetInstrumentFormKey(profile.Key),
            GuaranteeDocumentType.GuaranteeInstrument,
            profile.Key,
            profile.BankResourceKey,
            GetInstrumentTitleResourceKey(profile.Key),
            GetInstrumentSummaryResourceKey(profile.Key),
            GuaranteeDocumentFormStructuralClassKeys.OriginalInstrument,
            profile.CanonicalBankName,
            profile.ReferencePrefix,
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
            profile.FileNameHints,
            profile.BankNameHints);
    }

    private static GuaranteeDocumentFormDefinition CreateGenericBankLetterForm()
    {
        return new GuaranteeDocumentFormDefinition(
            GuaranteeDocumentFormKeys.BankLetterGeneric,
            GuaranteeDocumentType.BankResponse,
            GenericProfile.Key,
            GenericProfile.BankResourceKey,
            "DocumentForm_BankLetter_Generic_Title",
            "DocumentForm_BankLetter_Generic_Summary",
            GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
            CanonicalBankName: null,
            ReferencePrefix: null,
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
            []);
    }

    private static GuaranteeDocumentFormDefinition CreateBankLetterForm(GuaranteeDocumentBankProfileDefinition profile)
    {
        return new GuaranteeDocumentFormDefinition(
            GetBankLetterFormKey(profile.Key),
            GuaranteeDocumentType.BankResponse,
            profile.Key,
            profile.BankResourceKey,
            GetBankLetterTitleResourceKey(profile.Key),
            GetBankLetterSummaryResourceKey(profile.Key),
            GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
            profile.CanonicalBankName,
            profile.ReferencePrefix,
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
            profile.FileNameHints,
            profile.BankNameHints);
    }

    private static GuaranteeDocumentFormDefinition CreateSupportingAttachmentForm()
    {
        return new GuaranteeDocumentFormDefinition(
            GuaranteeDocumentFormKeys.SupportingAttachmentGeneric,
            GuaranteeDocumentType.SupportingDocument,
            GenericProfile.Key,
            GenericProfile.BankResourceKey,
            "DocumentForm_Attachment_Generic_Title",
            "DocumentForm_Attachment_Generic_Summary",
            GuaranteeDocumentFormStructuralClassKeys.SupportingAttachment,
            CanonicalBankName: null,
            ReferencePrefix: null,
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
            []);
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

    private static bool ContainsAny(string source, IReadOnlyList<string> fragments)
    {
        if (string.IsNullOrWhiteSpace(source) || fragments.Count == 0)
        {
            return false;
        }

        return fragments.Any(fragment => !string.IsNullOrWhiteSpace(fragment) && source.Contains(fragment, StringComparison.Ordinal));
    }

    private static string GetInstrumentFormKey(string profileKey)
    {
        return profileKey switch
        {
            GuaranteeDocumentBankProfileKeys.Snb => GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
            GuaranteeDocumentBankProfileKeys.AlRajhi => GuaranteeDocumentFormKeys.GuaranteeInstrumentAlRajhi,
            GuaranteeDocumentBankProfileKeys.Alinma => GuaranteeDocumentFormKeys.GuaranteeInstrumentAlinma,
            GuaranteeDocumentBankProfileKeys.Riyad => GuaranteeDocumentFormKeys.GuaranteeInstrumentRiyad,
            GuaranteeDocumentBankProfileKeys.BnpParibas => GuaranteeDocumentFormKeys.GuaranteeInstrumentBnpParibas,
            GuaranteeDocumentBankProfileKeys.Anb => GuaranteeDocumentFormKeys.GuaranteeInstrumentAnb,
            GuaranteeDocumentBankProfileKeys.Bsf => GuaranteeDocumentFormKeys.GuaranteeInstrumentBsf,
            GuaranteeDocumentBankProfileKeys.Sabb => GuaranteeDocumentFormKeys.GuaranteeInstrumentSabb,
            GuaranteeDocumentBankProfileKeys.Saib => GuaranteeDocumentFormKeys.GuaranteeInstrumentSaib,
            _ => throw new InvalidOperationException($"Unsupported bank profile '{profileKey}'.")
        };
    }

    private static string GetBankLetterFormKey(string profileKey)
    {
        return profileKey switch
        {
            GuaranteeDocumentBankProfileKeys.Snb => GuaranteeDocumentFormKeys.BankLetterSnb,
            GuaranteeDocumentBankProfileKeys.AlRajhi => GuaranteeDocumentFormKeys.BankLetterAlRajhi,
            GuaranteeDocumentBankProfileKeys.Alinma => GuaranteeDocumentFormKeys.BankLetterAlinma,
            GuaranteeDocumentBankProfileKeys.Riyad => GuaranteeDocumentFormKeys.BankLetterRiyad,
            GuaranteeDocumentBankProfileKeys.BnpParibas => GuaranteeDocumentFormKeys.BankLetterBnpParibas,
            GuaranteeDocumentBankProfileKeys.Anb => GuaranteeDocumentFormKeys.BankLetterAnb,
            GuaranteeDocumentBankProfileKeys.Bsf => GuaranteeDocumentFormKeys.BankLetterBsf,
            GuaranteeDocumentBankProfileKeys.Sabb => GuaranteeDocumentFormKeys.BankLetterSabb,
            GuaranteeDocumentBankProfileKeys.Saib => GuaranteeDocumentFormKeys.BankLetterSaib,
            _ => throw new InvalidOperationException($"Unsupported bank profile '{profileKey}'.")
        };
    }

    private static string GetInstrumentTitleResourceKey(string profileKey)
    {
        return profileKey switch
        {
            GuaranteeDocumentBankProfileKeys.Snb => "DocumentForm_Instrument_SNB_Title",
            GuaranteeDocumentBankProfileKeys.AlRajhi => "DocumentForm_Instrument_AlRajhi_Title",
            GuaranteeDocumentBankProfileKeys.Alinma => "DocumentForm_Instrument_Alinma_Title",
            GuaranteeDocumentBankProfileKeys.Riyad => "DocumentForm_Instrument_Riyad_Title",
            GuaranteeDocumentBankProfileKeys.BnpParibas => "DocumentForm_Instrument_BnpParibas_Title",
            GuaranteeDocumentBankProfileKeys.Anb => "DocumentForm_Instrument_ANB_Title",
            GuaranteeDocumentBankProfileKeys.Bsf => "DocumentForm_Instrument_BSF_Title",
            GuaranteeDocumentBankProfileKeys.Sabb => "DocumentForm_Instrument_SABB_Title",
            GuaranteeDocumentBankProfileKeys.Saib => "DocumentForm_Instrument_Saib_Title",
            _ => throw new InvalidOperationException($"Unsupported bank profile '{profileKey}'.")
        };
    }

    private static string GetInstrumentSummaryResourceKey(string profileKey)
    {
        return profileKey switch
        {
            GuaranteeDocumentBankProfileKeys.Snb => "DocumentForm_Instrument_SNB_Summary",
            GuaranteeDocumentBankProfileKeys.AlRajhi => "DocumentForm_Instrument_AlRajhi_Summary",
            GuaranteeDocumentBankProfileKeys.Alinma => "DocumentForm_Instrument_Alinma_Summary",
            GuaranteeDocumentBankProfileKeys.Riyad => "DocumentForm_Instrument_Riyad_Summary",
            GuaranteeDocumentBankProfileKeys.BnpParibas => "DocumentForm_Instrument_BnpParibas_Summary",
            GuaranteeDocumentBankProfileKeys.Anb => "DocumentForm_Instrument_ANB_Summary",
            GuaranteeDocumentBankProfileKeys.Bsf => "DocumentForm_Instrument_BSF_Summary",
            GuaranteeDocumentBankProfileKeys.Sabb => "DocumentForm_Instrument_SABB_Summary",
            GuaranteeDocumentBankProfileKeys.Saib => "DocumentForm_Instrument_Saib_Summary",
            _ => throw new InvalidOperationException($"Unsupported bank profile '{profileKey}'.")
        };
    }

    private static string GetBankLetterTitleResourceKey(string profileKey)
    {
        return profileKey switch
        {
            GuaranteeDocumentBankProfileKeys.Snb => "DocumentForm_BankLetter_SNB_Title",
            GuaranteeDocumentBankProfileKeys.AlRajhi => "DocumentForm_BankLetter_AlRajhi_Title",
            GuaranteeDocumentBankProfileKeys.Alinma => "DocumentForm_BankLetter_Alinma_Title",
            GuaranteeDocumentBankProfileKeys.Riyad => "DocumentForm_BankLetter_Riyad_Title",
            GuaranteeDocumentBankProfileKeys.BnpParibas => "DocumentForm_BankLetter_BnpParibas_Title",
            GuaranteeDocumentBankProfileKeys.Anb => "DocumentForm_BankLetter_ANB_Title",
            GuaranteeDocumentBankProfileKeys.Bsf => "DocumentForm_BankLetter_BSF_Title",
            GuaranteeDocumentBankProfileKeys.Sabb => "DocumentForm_BankLetter_SABB_Title",
            GuaranteeDocumentBankProfileKeys.Saib => "DocumentForm_BankLetter_Saib_Title",
            _ => throw new InvalidOperationException($"Unsupported bank profile '{profileKey}'.")
        };
    }

    private static string GetBankLetterSummaryResourceKey(string profileKey)
    {
        return profileKey switch
        {
            GuaranteeDocumentBankProfileKeys.Snb => "DocumentForm_BankLetter_SNB_Summary",
            GuaranteeDocumentBankProfileKeys.AlRajhi => "DocumentForm_BankLetter_AlRajhi_Summary",
            GuaranteeDocumentBankProfileKeys.Alinma => "DocumentForm_BankLetter_Alinma_Summary",
            GuaranteeDocumentBankProfileKeys.Riyad => "DocumentForm_BankLetter_Riyad_Summary",
            GuaranteeDocumentBankProfileKeys.BnpParibas => "DocumentForm_BankLetter_BnpParibas_Summary",
            GuaranteeDocumentBankProfileKeys.Anb => "DocumentForm_BankLetter_ANB_Summary",
            GuaranteeDocumentBankProfileKeys.Bsf => "DocumentForm_BankLetter_BSF_Summary",
            GuaranteeDocumentBankProfileKeys.Sabb => "DocumentForm_BankLetter_SABB_Summary",
            GuaranteeDocumentBankProfileKeys.Saib => "DocumentForm_BankLetter_Saib_Summary",
            _ => throw new InvalidOperationException($"Unsupported bank profile '{profileKey}'.")
        };
    }
}

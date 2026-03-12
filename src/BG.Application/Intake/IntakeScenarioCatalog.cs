using BG.Application.Models.Intake;
using BG.Domain.Guarantees;
using BG.Domain.Operations;

namespace BG.Application.Intake;

internal static class IntakeScenarioCatalog
{
    private static readonly IReadOnlyList<IntakeScenarioDefinition> Scenarios =
    [
        new(
            IntakeScenarioKind.NewGuarantee,
            IntakeScenarioKeys.NewGuarantee,
            "IntakeScenario_NewGuarantee_Title",
            "IntakeScenario_NewGuarantee_Summary",
            "IntakeScenario_NewGuarantee_SaveOutcome",
            "IntakeScenario_NewGuarantee_Handoff",
            GuaranteeDocumentType.GuaranteeInstrument,
            CorrespondenceKind: null,
            RequiresExistingGuarantee: false,
            ExpectedRequestType: null,
            ReviewCategory: OperationsReviewItemCategory.GuaranteeRegistration,
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
                new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0142", 99, true),
                new(IntakeFieldKeys.BankName, IntakeFieldKeys.BankName, "Saudi National Bank", 97, true),
                new(IntakeFieldKeys.Beneficiary, IntakeFieldKeys.Beneficiary, "King Faisal Specialist Hospital & Research Centre", 96, true),
                new(IntakeFieldKeys.Principal, IntakeFieldKeys.Principal, "Main Contractor Company", 94, true),
                new(IntakeFieldKeys.GuaranteeCategory, IntakeFieldKeys.GuaranteeCategory, "Contract", 100, true),
                new(IntakeFieldKeys.Amount, IntakeFieldKeys.Amount, "1250000", 94, true),
                new(IntakeFieldKeys.CurrencyCode, IntakeFieldKeys.CurrencyCode, "SAR", 99, true),
                new(IntakeFieldKeys.IssueDate, IntakeFieldKeys.IssueDate, "2026-03-11", 93, true),
                new(IntakeFieldKeys.ExpiryDate, IntakeFieldKeys.ExpiryDate, "2027-06-30", 92, true)
            ]),
        new(
            IntakeScenarioKind.ExtensionConfirmation,
            IntakeScenarioKeys.ExtensionConfirmation,
            "IntakeScenario_Extension_Title",
            "IntakeScenario_Extension_Summary",
            "IntakeScenario_Extension_SaveOutcome",
            "IntakeScenario_Extension_Handoff",
            GuaranteeDocumentType.BankResponse,
            GuaranteeCorrespondenceKind.BankConfirmation,
            RequiresExistingGuarantee: true,
            ExpectedRequestType: GuaranteeRequestType.Extend,
            ReviewCategory: OperationsReviewItemCategory.IncomingBankConfirmation,
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.NewExpiryDate,
                IntakeFieldKeys.BankReference
            ],
            [
                new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0142", 98, true),
                new(IntakeFieldKeys.BankReference, IntakeFieldKeys.BankReference, "EXT-44791", 93, true),
                new(IntakeFieldKeys.OfficialLetterDate, IntakeFieldKeys.OfficialLetterDate, "2026-03-15", 95, true),
                new(IntakeFieldKeys.NewExpiryDate, IntakeFieldKeys.NewExpiryDate, "2027-12-31", 91, true)
            ]),
        new(
            IntakeScenarioKind.ReductionConfirmation,
            IntakeScenarioKeys.ReductionConfirmation,
            "IntakeScenario_Reduction_Title",
            "IntakeScenario_Reduction_Summary",
            "IntakeScenario_Reduction_SaveOutcome",
            "IntakeScenario_Reduction_Handoff",
            GuaranteeDocumentType.BankResponse,
            GuaranteeCorrespondenceKind.BankConfirmation,
            RequiresExistingGuarantee: true,
            ExpectedRequestType: GuaranteeRequestType.Reduce,
            ReviewCategory: OperationsReviewItemCategory.IncomingBankConfirmation,
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.Amount,
                IntakeFieldKeys.BankReference
            ],
            [
                new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0142", 98, true),
                new(IntakeFieldKeys.BankReference, IntakeFieldKeys.BankReference, "RED-55122", 92, true),
                new(IntakeFieldKeys.OfficialLetterDate, IntakeFieldKeys.OfficialLetterDate, "2026-03-22", 94, true),
                new(IntakeFieldKeys.Amount, IntakeFieldKeys.Amount, "950000", 91, true)
            ]),
        new(
            IntakeScenarioKind.ReleaseConfirmation,
            IntakeScenarioKeys.ReleaseConfirmation,
            "IntakeScenario_Release_Title",
            "IntakeScenario_Release_Summary",
            "IntakeScenario_Release_SaveOutcome",
            "IntakeScenario_Release_Handoff",
            GuaranteeDocumentType.BankResponse,
            GuaranteeCorrespondenceKind.BankConfirmation,
            RequiresExistingGuarantee: true,
            ExpectedRequestType: GuaranteeRequestType.Release,
            ReviewCategory: OperationsReviewItemCategory.IncomingBankConfirmation,
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.BankReference
            ],
            [
                new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0142", 98, true),
                new(IntakeFieldKeys.BankReference, IntakeFieldKeys.BankReference, "REL-13210", 92, true),
                new(IntakeFieldKeys.OfficialLetterDate, IntakeFieldKeys.OfficialLetterDate, "2026-04-02", 94, true)
            ]),
        new(
            IntakeScenarioKind.StatusVerification,
            IntakeScenarioKeys.StatusVerification,
            "IntakeScenario_Status_Title",
            "IntakeScenario_Status_Summary",
            "IntakeScenario_Status_SaveOutcome",
            "IntakeScenario_Status_Handoff",
            GuaranteeDocumentType.BankResponse,
            GuaranteeCorrespondenceKind.BankStatusReply,
            RequiresExistingGuarantee: true,
            ExpectedRequestType: GuaranteeRequestType.VerifyStatus,
            ReviewCategory: OperationsReviewItemCategory.IncomingStatusReply,
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.OfficialLetterDate,
                IntakeFieldKeys.BankReference,
                IntakeFieldKeys.StatusStatement
            ],
            [
                new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0142", 98, true),
                new(IntakeFieldKeys.BankReference, IntakeFieldKeys.BankReference, "STAT-8810", 90, true),
                new(IntakeFieldKeys.OfficialLetterDate, IntakeFieldKeys.OfficialLetterDate, "2026-04-10", 95, true),
                new(IntakeFieldKeys.StatusStatement, IntakeFieldKeys.StatusStatement, "Guarantee remains active without change.", 88, true)
            ]),
        new(
            IntakeScenarioKind.SupportingAttachment,
            IntakeScenarioKeys.SupportingAttachment,
            "IntakeScenario_Attachment_Title",
            "IntakeScenario_Attachment_Summary",
            "IntakeScenario_Attachment_SaveOutcome",
            "IntakeScenario_Attachment_Handoff",
            GuaranteeDocumentType.SupportingDocument,
            CorrespondenceKind: null,
            RequiresExistingGuarantee: true,
            ExpectedRequestType: null,
            ReviewCategory: OperationsReviewItemCategory.SupportingDocumentation,
            [
                IntakeFieldKeys.GuaranteeNumber,
                IntakeFieldKeys.AttachmentNote
            ],
            [
                new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0142", 98, true),
                new(IntakeFieldKeys.AttachmentNote, IntakeFieldKeys.AttachmentNote, "Original signed annex attached.", 85, true)
            ])
    ];

    public static IReadOnlyList<IntakeScenarioDefinition> GetAll()
    {
        return Scenarios;
    }

    public static IntakeScenarioDefinition GetDefault()
    {
        return Scenarios[0];
    }

    public static IntakeScenarioDefinition? Find(string? scenarioKey)
    {
        if (string.IsNullOrWhiteSpace(scenarioKey))
        {
            return null;
        }

        return Scenarios.FirstOrDefault(
            scenario => string.Equals(scenario.Key, scenarioKey.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

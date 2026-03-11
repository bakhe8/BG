using BG.Application.Contracts.Services;
using BG.Application.Models.Intake;

namespace BG.Application.Services;

internal sealed class IntakeWorkspaceService : IIntakeWorkspaceService
{
    private static readonly IntakeScenarioSnapshotDto[] Scenarios =
    [
        new(
            "new-guarantee",
            "IntakeScenario_NewGuarantee_Title",
            "IntakeScenario_NewGuarantee_Summary",
            "IntakeScenario_NewGuarantee_SaveOutcome",
            "IntakeScenario_NewGuarantee_Handoff",
            [
                "IntakeField_GuaranteeNumber",
                "IntakeField_BankName",
                "IntakeField_Amount",
                "IntakeField_ExpiryDate",
                "IntakeField_DocumentType"
            ],
            [
                new("IntakeField_GuaranteeNumber", "BG-2026-0142", 99, true),
                new("IntakeField_BankName", "Saudi National Bank", 97, true),
                new("IntakeField_Beneficiary", "King Faisal Specialist Hospital", 96, true),
                new("IntakeField_Amount", "1,250,000 SAR", 94, true),
                new("IntakeField_ExpiryDate", "2027-06-30", 92, true)
            ]),
        new(
            "extension-confirmation",
            "IntakeScenario_Extension_Title",
            "IntakeScenario_Extension_Summary",
            "IntakeScenario_Extension_SaveOutcome",
            "IntakeScenario_Extension_Handoff",
            [
                "IntakeField_GuaranteeNumber",
                "IntakeField_OfficialLetterDate",
                "IntakeField_NewExpiryDate",
                "IntakeField_BankReference"
            ],
            [
                new("IntakeField_GuaranteeNumber", "BG-2026-0142", 98, true),
                new("IntakeField_BankReference", "EXT-44791", 93, true),
                new("IntakeField_OfficialLetterDate", "2026-03-15", 95, true),
                new("IntakeField_NewExpiryDate", "2027-12-31", 91, true)
            ]),
        new(
            "release-confirmation",
            "IntakeScenario_Release_Title",
            "IntakeScenario_Release_Summary",
            "IntakeScenario_Release_SaveOutcome",
            "IntakeScenario_Release_Handoff",
            [
                "IntakeField_GuaranteeNumber",
                "IntakeField_OfficialLetterDate",
                "IntakeField_BankReference",
                "IntakeField_DocumentType"
            ],
            [
                new("IntakeField_GuaranteeNumber", "BG-2026-0142", 98, true),
                new("IntakeField_BankReference", "REL-13210", 92, true),
                new("IntakeField_OfficialLetterDate", "2026-04-02", 94, true),
                new("IntakeField_DocumentType", "Bank release confirmation", 99, true)
            ]),
        new(
            "status-verification",
            "IntakeScenario_Status_Title",
            "IntakeScenario_Status_Summary",
            "IntakeScenario_Status_SaveOutcome",
            "IntakeScenario_Status_Handoff",
            [
                "IntakeField_GuaranteeNumber",
                "IntakeField_OfficialLetterDate",
                "IntakeField_BankReference",
                "IntakeField_StatusStatement"
            ],
            [
                new("IntakeField_GuaranteeNumber", "BG-2026-0142", 98, true),
                new("IntakeField_BankReference", "STAT-8810", 90, true),
                new("IntakeField_OfficialLetterDate", "2026-04-10", 95, true),
                new("IntakeField_StatusStatement", "Guarantee remains active without change.", 88, true)
            ]),
        new(
            "supporting-attachment",
            "IntakeScenario_Attachment_Title",
            "IntakeScenario_Attachment_Summary",
            "IntakeScenario_Attachment_SaveOutcome",
            "IntakeScenario_Attachment_Handoff",
            [
                "IntakeField_GuaranteeNumber",
                "IntakeField_DocumentType",
                "IntakeField_AttachmentNote"
            ],
            [
                new("IntakeField_GuaranteeNumber", "BG-2026-0142", 98, true),
                new("IntakeField_DocumentType", "Supporting attachment", 96, true),
                new("IntakeField_AttachmentNote", "Original signed annex attached.", 85, true)
            ])
    ];

    public IntakeWorkspaceSnapshotDto GetWorkspace(string? scenarioKey = null)
    {
        var selectedScenario = Scenarios.FirstOrDefault(
                scenario => string.Equals(scenario.Key, scenarioKey, StringComparison.OrdinalIgnoreCase))
            ?? Scenarios[0];

        return new IntakeWorkspaceSnapshotDto(
            "IntakeWorkspace_PrimaryRole",
            "IntakeWorkspace_SaveMode",
            "IntakeWorkspace_ReviewGate",
            [
                "IntakeAction_Scan",
                "IntakeAction_Classify",
                "IntakeAction_Verify",
                "IntakeAction_Save"
            ],
            [
                "IntakeExcluded_RequestActions",
                "IntakeExcluded_Printing",
                "IntakeExcluded_Approvals",
                "IntakeExcluded_Dispatch"
            ],
            [
                "IntakePipeline_ClassifyPage",
                "IntakePipeline_DirectPdfText",
                "IntakePipeline_ImagePreprocessing",
                "IntakePipeline_OcrAndLayout",
                "IntakePipeline_PostProcessing",
                "IntakePipeline_HumanReview",
                "IntakePipeline_SaveAndHandoff"
            ],
            [
                "IntakeQuality_TextFirst",
                "IntakeQuality_Confidence",
                "IntakeQuality_SingleSave",
                "IntakeQuality_SourceDocument",
                "IntakeQuality_BankSpecificRules"
            ],
            [
                "IntakeFuture_ScanStation",
                "IntakeFuture_ScanToFolder",
                "IntakeFuture_DeviceApi"
            ],
            Scenarios,
            selectedScenario);
    }
}

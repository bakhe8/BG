using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.Application.Operations;

public static class RequestWorkflowTemplateCatalog
{
    private static readonly IReadOnlyList<RequestWorkflowTemplateDto> Templates =
    [
        Create(
            GuaranteeRequestType.Extend,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Extend_Title",
            "WorkflowTemplate_Extend_Summary",
            CreateCoreApprovalStages()),
        Create(
            GuaranteeRequestType.Reduce,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Reduce_Title",
            "WorkflowTemplate_Reduce_Summary",
            CreateCoreApprovalStages()),
        Create(
            GuaranteeRequestType.ReplaceWithReducedGuarantee,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Replacement_Title",
            "WorkflowTemplate_Replacement_Summary",
            CreateCoreApprovalStages()),
        Create(
            GuaranteeRequestType.VerifyStatus,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_VerifyStatus_Title",
            "WorkflowTemplate_VerifyStatus_Summary",
            CreateCoreApprovalStages()),
        Create(
            GuaranteeRequestType.Release,
            GuaranteeCategory.Contract,
            "GuaranteeCategory_Contract",
            "WorkflowTemplate_ReleaseContract_Title",
            "WorkflowTemplate_ReleaseContract_Summary",
            [
                ..CreateCoreApprovalStages(),
                CreateSignatureStage(5, "WorkflowStage_ContractsSigner1_Title", "WorkflowStage_ContractsSigner1_Summary", ApprovalDelegationPolicy.DirectSignerRequired),
                CreateSignatureStage(6, "WorkflowStage_ContractsSigner2_Title", "WorkflowStage_ContractsSigner2_Summary", ApprovalDelegationPolicy.DirectSignerRequired),
                CreateSignatureStage(7, "WorkflowStage_ContractsSigner3_Title", "WorkflowStage_ContractsSigner3_Summary", ApprovalDelegationPolicy.DirectSignerRequired),
                CreateSignatureStage(8, "WorkflowStage_ExecutiveVicePresident_Title", "WorkflowStage_ExecutiveVicePresident_Summary", ApprovalDelegationPolicy.DirectSignerRequired)
            ]),
        Create(
            GuaranteeRequestType.Release,
            GuaranteeCategory.PurchaseOrder,
            "GuaranteeCategory_PurchaseOrder",
            "WorkflowTemplate_ReleasePurchaseOrder_Title",
            "WorkflowTemplate_ReleasePurchaseOrder_Summary",
            [
                ..CreateCoreApprovalStages(),
                CreateSignatureStage(5, "WorkflowStage_ProcurementSigner1_Title", "WorkflowStage_ProcurementSigner1_Summary", ApprovalDelegationPolicy.DirectSignerRequired),
                CreateSignatureStage(6, "WorkflowStage_ProcurementSigner2_Title", "WorkflowStage_ProcurementSigner2_Summary", ApprovalDelegationPolicy.DirectSignerRequired),
                CreateSignatureStage(7, "WorkflowStage_ProcurementSigner3_Title", "WorkflowStage_ProcurementSigner3_Summary", ApprovalDelegationPolicy.DirectSignerRequired),
                CreateSignatureStage(8, "WorkflowStage_ExecutiveVicePresident_Title", "WorkflowStage_ExecutiveVicePresident_Summary", ApprovalDelegationPolicy.DirectSignerRequired)
            ])
    ];

    public static IReadOnlyList<RequestWorkflowTemplateDto> GetAll()
    {
        return Templates;
    }

    public static RequestWorkflowTemplateDto? Resolve(GuaranteeRequestType requestType, GuaranteeCategory? guaranteeCategory)
    {
        if (requestType == GuaranteeRequestType.Release)
        {
            if (guaranteeCategory is null)
            {
                return null;
            }

            return Templates.SingleOrDefault(template =>
                template.RequestType == requestType &&
                template.GuaranteeCategory == guaranteeCategory);
        }

        return Templates.SingleOrDefault(template =>
            template.RequestType == requestType &&
            template.GuaranteeCategory is null);
    }

    private static IReadOnlyList<RequestWorkflowStageTemplateDto> CreateCoreApprovalStages()
    {
        return
        [
            CreateSignatureStage(1, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary"),
            CreateSignatureStage(2, "WorkflowStage_DepartmentManager_Title", "WorkflowStage_DepartmentManager_Summary"),
            CreateSignatureStage(3, "WorkflowStage_ProgramDirector_Title", "WorkflowStage_ProgramDirector_Summary"),
            CreateSignatureStage(4, "WorkflowStage_DeputyFinancialAffairsDirector_Title", "WorkflowStage_DeputyFinancialAffairsDirector_Summary", ApprovalDelegationPolicy.DirectSignerRequired)
        ];
    }

    private static RequestWorkflowStageTemplateDto CreateSignatureStage(
        int level,
        string titleResourceKey,
        string summaryResourceKey,
        ApprovalDelegationPolicy delegationPolicy = ApprovalDelegationPolicy.Inherit)
    {
        return new RequestWorkflowStageTemplateDto(
            level,
            titleResourceKey,
            summaryResourceKey,
            RequiresLetterSignature: true,
            CurrentSignatureModeResourceKey: "WorkflowSignatureMode_ButtonStampedPdf",
            FinalPdfEffectResourceKey: "WorkflowSignatureEffect_FinalLetterPdf",
            DelegationPolicy: delegationPolicy);
    }

    private static RequestWorkflowTemplateDto Create(
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        string? guaranteeCategoryResourceKey,
        string titleResourceKey,
        string summaryResourceKey,
        IReadOnlyList<RequestWorkflowStageTemplateDto> stages)
    {
        var key = guaranteeCategory is null
            ? requestType.ToString()
            : $"{requestType}:{guaranteeCategory}";

        return new RequestWorkflowTemplateDto(
            key,
            requestType,
            guaranteeCategory,
            guaranteeCategoryResourceKey,
            titleResourceKey,
            summaryResourceKey,
            stages,
            FinalSignatureDelegationPolicy: ApprovalDelegationPolicy.Inherit,
            DelegationAmountThreshold: null);
    }
}

namespace BG.Application.Workflow;

public static class WorkflowErrorCodes
{
    public const string DefinitionNotFound = "workflow.definition_not_found";
    public const string StageNotFound = "workflow.stage_not_found";
    public const string RoleRequired = "workflow.role_required";
    public const string RoleNotFound = "workflow.role_not_found";
    public const string StageAlreadyFirst = "workflow.stage_already_first";
    public const string StageAlreadyLast = "workflow.stage_already_last";
    public const string DefinitionRequiresStage = "workflow.definition_requires_stage";
    public const string DelegationAmountThresholdInvalid = "workflow.delegation_amount_threshold_invalid";
}

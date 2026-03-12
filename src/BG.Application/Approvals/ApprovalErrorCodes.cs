namespace BG.Application.Approvals;

public static class ApprovalErrorCodes
{
    public const string ApproverContextRequired = "approvals.approver_context_required";
    public const string ApproverContextInvalid = "approvals.approver_context_invalid";
    public const string RequestNotFound = "approvals.request_not_found";
    public const string RequestNotActionable = "approvals.request_not_actionable";
    public const string GovernancePolicyBlocked = "approvals.governance_policy_blocked";
    public const string GovernanceSignerReuseBlocked = "approvals.governance_signer_reuse_blocked";
}

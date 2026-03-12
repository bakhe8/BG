namespace BG.Application.Approvals;

public static class ApprovalDelegationErrorCodes
{
    public const string DelegatorRequired = "approvals.delegation_delegator_required";
    public const string DelegateRequired = "approvals.delegation_delegate_required";
    public const string RoleRequired = "approvals.delegation_role_required";
    public const string SameUserNotAllowed = "approvals.delegation_same_user_not_allowed";
    public const string UserNotFound = "approvals.delegation_user_not_found";
    public const string RoleNotFound = "approvals.delegation_role_not_found";
    public const string DelegatorRoleRequired = "approvals.delegation_delegator_role_required";
    public const string InvalidPeriod = "approvals.delegation_invalid_period";
    public const string Overlap = "approvals.delegation_overlap";
    public const string DelegationNotFound = "approvals.delegation_not_found";
    public const string DelegationAlreadyRevoked = "approvals.delegation_already_revoked";
}

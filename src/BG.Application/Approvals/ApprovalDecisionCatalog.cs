using BG.Domain.Guarantees;

namespace BG.Application.Approvals;

internal static class ApprovalDecisionCatalog
{
    public static string GetResourceKey(ApprovalDecisionOutcome outcome)
    {
        return outcome switch
        {
            ApprovalDecisionOutcome.Approved => "ApprovalDecision_Approved",
            ApprovalDecisionOutcome.Returned => "ApprovalDecision_Returned",
            ApprovalDecisionOutcome.Rejected => "ApprovalDecision_Rejected",
            _ => "ApprovalDecision_Approved"
        };
    }

    public static GuaranteeEventType GetLedgerEventType(ApprovalDecisionOutcome outcome)
    {
        return outcome switch
        {
            ApprovalDecisionOutcome.Approved => GuaranteeEventType.ApprovalApproved,
            ApprovalDecisionOutcome.Returned => GuaranteeEventType.ApprovalReturned,
            ApprovalDecisionOutcome.Rejected => GuaranteeEventType.ApprovalRejected,
            _ => throw new InvalidOperationException("Unsupported approval outcome.")
        };
    }

    public static bool RequiresImmediateRequestTransition(ApprovalDecisionOutcome outcome)
    {
        return outcome is ApprovalDecisionOutcome.Returned or ApprovalDecisionOutcome.Rejected;
    }

    public static string? TryGetExecutionModeResourceKey(string? executionMode)
    {
        return executionMode switch
        {
            nameof(ApprovalLedgerExecutionMode.Direct) => "ApprovalExecutionMode_Direct",
            nameof(ApprovalLedgerExecutionMode.Delegated) => "ApprovalExecutionMode_Delegated",
            _ => null
        };
    }

    public static string? TryGetResourceKeyForRequestStatus(GuaranteeRequestStatus status)
    {
        return status switch
        {
            GuaranteeRequestStatus.Returned => GetResourceKey(ApprovalDecisionOutcome.Returned),
            GuaranteeRequestStatus.Rejected => GetResourceKey(ApprovalDecisionOutcome.Rejected),
            GuaranteeRequestStatus.ApprovedForDispatch => GetResourceKey(ApprovalDecisionOutcome.Approved),
            _ => null
        };
    }
}

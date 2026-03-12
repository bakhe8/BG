using BG.Domain.Workflow;

namespace BG.Application.Approvals;

internal static class ApprovalDelegationStatusCatalog
{
    public static string GetResourceKey(ApprovalDelegation delegation, DateTimeOffset effectiveAtUtc)
    {
        if (delegation.RevokedAtUtc.HasValue)
        {
            return "ApprovalDelegation_StatusRevoked";
        }

        if (delegation.StartsAtUtc > effectiveAtUtc)
        {
            return "ApprovalDelegation_StatusScheduled";
        }

        if (delegation.EndsAtUtc < effectiveAtUtc)
        {
            return "ApprovalDelegation_StatusExpired";
        }

        return "ApprovalDelegation_StatusActive";
    }
}

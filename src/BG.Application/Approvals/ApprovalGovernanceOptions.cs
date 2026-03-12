using BG.Domain.Guarantees;

namespace BG.Application.Approvals;

public sealed class ApprovalGovernanceOptions
{
    public const string SectionName = "ApprovalGovernance";

    public bool RequireSignerSeparation { get; set; } = true;

    public bool RequireDirectSignerForFinalSignatureStage { get; set; } = true;

    public GuaranteeRequestType[] DirectSignerOnlyRequestTypes { get; set; } =
    [
        GuaranteeRequestType.Release
    ];
}

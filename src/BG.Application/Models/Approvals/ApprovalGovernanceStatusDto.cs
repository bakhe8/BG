namespace BG.Application.Models.Approvals;

public sealed record ApprovalGovernanceStatusDto(
    bool IsDecisionBlocked,
    string? ReasonResourceKey,
    string? PolicyResourceKey,
    string? ConflictingStageTitleResourceKey,
    string? ConflictingStageTitle,
    string? ConflictingStageRoleName,
    string? ConflictingActorDisplayName,
    string? ConflictingResponsibleSignerDisplayName);

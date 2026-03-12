namespace BG.Application.Models.Approvals;

public sealed record ApprovalPriorSignatureDto(
    Guid StageId,
    int Sequence,
    string? StageTitleResourceKey,
    string? StageTitle,
    string? StageRoleName,
    DateTimeOffset ActedAtUtc,
    string? ActorDisplayName,
    string? ResponsibleSignerDisplayName,
    bool WasDelegated);

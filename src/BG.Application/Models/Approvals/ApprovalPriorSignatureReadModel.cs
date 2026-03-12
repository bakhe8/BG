namespace BG.Application.Models.Approvals;

public sealed record ApprovalPriorSignatureReadModel(
    Guid StageId,
    int Sequence,
    string? StageTitleResourceKey,
    string? StageTitle,
    string? StageRoleName,
    DateTimeOffset ActedAtUtc,
    Guid? ActorUserId,
    string? ActorDisplayName,
    Guid? ResponsibleSignerUserId,
    string? ResponsibleSignerDisplayName);

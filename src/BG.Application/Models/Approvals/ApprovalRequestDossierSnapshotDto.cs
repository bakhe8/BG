namespace BG.Application.Models.Approvals;

public sealed record ApprovalRequestDossierSnapshotDto(
    ApprovalActorSummaryDto? ActiveActor,
    IReadOnlyList<ApprovalActorSummaryDto> AvailableActors,
    ApprovalQueueItemDto? Item,
    bool HasEligibleActor,
    string? ContextNoticeResourceKey,
    string? UnavailableResourceKey);

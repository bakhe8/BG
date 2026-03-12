using BG.Application.Common;

namespace BG.Application.Models.Approvals;

public sealed record ApprovalQueueSnapshotDto(
    ApprovalActorSummaryDto? ActiveActor,
    IReadOnlyList<ApprovalActorSummaryDto> AvailableActors,
    IReadOnlyList<ApprovalQueueItemDto> Items,
    PageInfoDto ItemsPage,
    bool HasEligibleActor,
    string? ContextNoticeResourceKey);

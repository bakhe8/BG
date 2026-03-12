using BG.Application.Operations;
using BG.Application.Common;

namespace BG.Application.Models.Requests;

public sealed record RequestWorkspaceSnapshotDto(
    RequestActorSummaryDto? ActiveActor,
    IReadOnlyList<RequestActorSummaryDto> AvailableActors,
    IReadOnlyList<RequestSummaryDto> OwnedRequests,
    PageInfoDto OwnedRequestsPage,
    IReadOnlyList<RequestWorkflowTemplateDto> WorkflowTemplates,
    bool HasEligibleActor,
    string? ContextNoticeResourceKey);

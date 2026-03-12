namespace BG.Application.Models.Approvals;

public sealed record ApprovalActorSummaryDto(
    Guid Id,
    string Username,
    string DisplayName);

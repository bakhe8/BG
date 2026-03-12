namespace BG.Application.Models.Requests;

public sealed record RequestActorSummaryDto(
    Guid Id,
    string Username,
    string DisplayName);

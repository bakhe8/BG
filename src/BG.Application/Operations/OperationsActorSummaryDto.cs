namespace BG.Application.Operations;

public sealed record OperationsActorSummaryDto(
    Guid Id,
    string Username,
    string DisplayName);

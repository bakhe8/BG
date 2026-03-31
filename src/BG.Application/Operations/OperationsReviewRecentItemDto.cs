namespace BG.Application.Operations;

public sealed record OperationsReviewRecentItemDto(
    Guid Id,
    string GuaranteeNumber,
    string ScenarioTitleResourceKey,
    string StatusResourceKey,
    DateTimeOffset CompletedAtUtc);

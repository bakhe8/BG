namespace BG.Application.Operations;

public sealed record OperationsReviewRecentItemReadModel(
    Guid Id,
    string GuaranteeNumber,
    string ScenarioKey,
    DateTimeOffset CompletedAtUtc);

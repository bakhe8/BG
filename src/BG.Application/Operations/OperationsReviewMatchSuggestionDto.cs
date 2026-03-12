namespace BG.Application.Operations;

public sealed record OperationsReviewMatchSuggestionDto(
    Guid RequestId,
    string RequestTypeResourceKey,
    string StatusResourceKey,
    int Score,
    string ConfidenceResourceKey,
    DateTimeOffset? SubmittedToBankAtUtc,
    string? OutgoingReferenceNumber,
    IReadOnlyList<string> ReasonResourceKeys);

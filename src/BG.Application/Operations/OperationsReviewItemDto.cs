using BG.Application.Models.Documents;

namespace BG.Application.Operations;

public sealed record OperationsReviewItemDto(
    Guid Id,
    string GuaranteeNumber,
    string ScenarioKey,
    string ScenarioTitleResourceKey,
    string CategoryResourceKey,
    string StatusResourceKey,
    string RecommendedLaneResourceKey,
    string FileName,
    string? BankReference,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CapturedAtUtc,
    string? CapturedByDisplayName,
    string CaptureChannelResourceKey,
    string? SourceSystemName,
    string? SourceReference,
    GuaranteeDocumentFormSnapshotDto? DocumentForm,
    bool SupportsRequestMatching,
    IReadOnlyList<OperationsReviewMatchSuggestionDto> MatchSuggestions,
    string? SuggestedConfirmedExpiryDate,
    string? SuggestedConfirmedAmount,
    string? SuggestedStatusStatement,
    bool RequiresConfirmedExpiryDate,
    bool RequiresConfirmedAmount,
    DateTimeOffset? CompletedAtUtc);

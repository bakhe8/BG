using BG.Domain.Guarantees;
using BG.Domain.Operations;

namespace BG.Application.Operations;

public sealed record OperationsReviewQueueItemReadModel(
    Guid Id,
    Guid GuaranteeId,
    string GuaranteeNumber,
    string ScenarioKey,
    OperationsReviewItemCategory Category,
    OperationsReviewItemStatus Status,
    string FileName,
    string? BankReference,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CapturedAtUtc,
    string? CapturedByDisplayName,
    GuaranteeDocumentCaptureChannel CaptureChannel,
    string? SourceSystemName,
    string? SourceReference,
    string? VerifiedDataJson,
    DateOnly? BankLetterDate,
    IReadOnlyList<OperationsReviewRequestCandidateReadModel> CandidateRequests);

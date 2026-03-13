using BG.Domain.Guarantees;

namespace BG.Application.Operations;

public sealed record OperationsReviewRequestCandidateReadModel(
    Guid RequestId,
    GuaranteeRequestType RequestType,
    GuaranteeRequestStatus Status,
    DateOnly? RequestedExpiryDate,
    decimal? RequestedAmount,
    DateTimeOffset? SubmittedToBankAtUtc,
    string? LatestOutgoingReferenceNumber,
    GuaranteeDocumentType? PrimaryDocumentType,
    string? PrimaryDocumentVerifiedDataJson);

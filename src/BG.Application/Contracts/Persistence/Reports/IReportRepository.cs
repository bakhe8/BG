using BG.Application.Models.Reports;
using BG.Domain.Guarantees;

namespace BG.Application.Contracts.Persistence.Reports;

public interface IReportRepository
{
    Task<IReadOnlyList<GuaranteePortfolioItemDto>> GetPortfolioAsync(
        GuaranteeStatus? status,
        GuaranteeCategory? category,
        string? bankName,
        DateOnly? issuedFrom,
        DateOnly? issuedTo,
        CancellationToken ct);

    Task<IReadOnlyList<ExpiringGuaranteeItemDto>> GetExpiringAsync(
        int withinDays,
        DateOnly today,
        CancellationToken ct);

    Task<IReadOnlyList<RequestActivityItemDto>> GetRequestActivityAsync(
        GuaranteeRequestStatus? status,
        GuaranteeRequestType? requestType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct);
}

using BG.Application.Models.Reports;
using BG.Domain.Guarantees;

namespace BG.Application.Contracts.Services.Reports;

public interface IReportService
{
    Task<IReadOnlyList<GuaranteePortfolioItemDto>> GetPortfolioAsync(
        GuaranteeStatus? status,
        GuaranteeCategory? category,
        string? bankName,
        DateOnly? issuedFrom,
        DateOnly? issuedTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpiringGuaranteeItemDto>> GetExpiringAsync(
        int withinDays,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequestActivityItemDto>> GetRequestActivityAsync(
        GuaranteeRequestStatus? status,
        GuaranteeRequestType? requestType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);
}

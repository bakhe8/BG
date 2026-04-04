using BG.Application.Contracts.Persistence.Reports;
using BG.Application.Contracts.Services.Reports;
using BG.Application.Models.Reports;
using BG.Domain.Guarantees;

namespace BG.Application.Services.Reports;

internal sealed class ReportService : IReportService
{
    private readonly IReportRepository _repository;

    public ReportService(IReportRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<GuaranteePortfolioItemDto>> GetPortfolioAsync(
        GuaranteeStatus? status,
        GuaranteeCategory? category,
        string? bankName,
        DateOnly? issuedFrom,
        DateOnly? issuedTo,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetPortfolioAsync(status, category, bankName, issuedFrom, issuedTo, cancellationToken);
    }

    public Task<IReadOnlyList<ExpiringGuaranteeItemDto>> GetExpiringAsync(
        int withinDays,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _repository.GetExpiringAsync(withinDays, today, cancellationToken);
    }

    public Task<IReadOnlyList<RequestActivityItemDto>> GetRequestActivityAsync(
        GuaranteeRequestStatus? status,
        GuaranteeRequestType? requestType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetRequestActivityAsync(status, requestType, from, to, cancellationToken);
    }
}

using BG.Application.Contracts.Persistence.Reports;
using BG.Application.Models.Reports;
using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class ReportRepository : IReportRepository
{
    private readonly BgDbContext _dbContext;

    public ReportRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GuaranteePortfolioItemDto>> GetPortfolioAsync(
        GuaranteeStatus? status,
        GuaranteeCategory? category,
        string? bankName,
        DateOnly? issuedFrom,
        DateOnly? issuedTo,
        CancellationToken ct)
    {
        var query = _dbContext.Guarantees.AsNoTracking();

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        if (category.HasValue)
            query = query.Where(g => g.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(bankName))
            query = query.Where(g => g.BankName.Contains(bankName));

        if (issuedFrom.HasValue)
            query = query.Where(g => g.IssueDate >= issuedFrom.Value);

        if (issuedTo.HasValue)
            query = query.Where(g => g.IssueDate <= issuedTo.Value);

        return await query
            .OrderByDescending(g => g.IssueDate)
            .Select(g => new GuaranteePortfolioItemDto(
                g.GuaranteeNumber,
                g.BankName,
                g.BeneficiaryName,
                g.PrincipalName,
                g.Category,
                g.Status,
                g.CurrentAmount,
                g.CurrencyCode,
                g.IssueDate,
                g.ExpiryDate))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExpiringGuaranteeItemDto>> GetExpiringAsync(
        int withinDays,
        DateOnly today,
        CancellationToken ct)
    {
        var cutoff = today.AddDays(withinDays);

        return await _dbContext.Guarantees
            .AsNoTracking()
            .Where(g => g.Status == GuaranteeStatus.Active &&
                        g.ExpiryDate >= today &&
                        g.ExpiryDate <= cutoff)
            .OrderBy(g => g.ExpiryDate)
            .Select(g => new ExpiringGuaranteeItemDto(
                g.GuaranteeNumber,
                g.BankName,
                g.BeneficiaryName,
                g.CurrentAmount,
                g.CurrencyCode,
                g.ExpiryDate,
                g.ExpiryDate.DayNumber - today.DayNumber))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RequestActivityItemDto>> GetRequestActivityAsync(
        GuaranteeRequestStatus? status,
        GuaranteeRequestType? requestType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = _dbContext.GuaranteeRequests
            .AsNoTracking()
            .Include(r => r.Guarantee)
            .Include(r => r.RequestedByUser)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (requestType.HasValue)
            query = query.Where(r => r.RequestType == requestType.Value);

        if (from.HasValue)
            query = query.Where(r => r.CreatedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.CreatedAtUtc <= to.Value);

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new RequestActivityItemDto(
                r.Id,
                r.Guarantee.GuaranteeNumber,
                r.RequestType,
                r.Status,
                r.RequestedByUser.DisplayName,
                r.CreatedAtUtc,
                r.CompletedAtUtc))
            .ToListAsync(ct);
    }
}

using BG.Application.Contracts.Persistence;
using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class GuaranteeHistoryRepository : IGuaranteeHistoryRepository
{
    private readonly BgDbContext _dbContext;

    public GuaranteeHistoryRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guarantee?> GetGuaranteeWithEventsAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Guarantees
            .AsNoTracking()
            .Include(guarantee => guarantee.Events.OrderByDescending(ledgerEntry => ledgerEntry.OccurredAtUtc))
            .Include(guarantee => guarantee.Requests)
            .FirstOrDefaultAsync(guarantee => guarantee.GuaranteeNumber == guaranteeNumber, cancellationToken);
    }
}

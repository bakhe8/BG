using BG.Application.Contracts.Persistence;
using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class BankRepository : IBankRepository
{
    private readonly BgDbContext _dbContext;

    public BankRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Bank>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Banks
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Bank?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Banks
            .SingleOrDefaultAsync(bank => bank.Id == id, cancellationToken);
    }

    public Task<Bank?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var normalizedShortCode = Bank.NormalizeShortCodeKey(shortCode);

        return _dbContext.Banks
            .SingleOrDefaultAsync(bank => bank.ShortCode == normalizedShortCode, cancellationToken);
    }

    public Task<Bank?> GetByCanonicalNameAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        var normalizedCanonicalName = canonicalName.Trim();

        return _dbContext.Banks
            .SingleOrDefaultAsync(
                bank => bank.CanonicalName.ToUpper() == normalizedCanonicalName.ToUpper(),
                cancellationToken);
    }

    public Task AddAsync(Bank bank, CancellationToken cancellationToken = default)
    {
        return _dbContext.Banks.AddAsync(bank, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

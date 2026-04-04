using BG.Domain.Guarantees;

namespace BG.Application.Contracts.Persistence;

public interface IBankRepository
{
    Task<IReadOnlyList<Bank>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<Bank?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Bank?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<Bank?> GetByCanonicalNameAsync(string canonicalName, CancellationToken cancellationToken = default);

    Task AddAsync(Bank bank, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

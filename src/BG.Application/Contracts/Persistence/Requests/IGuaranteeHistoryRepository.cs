using BG.Domain.Guarantees;

namespace BG.Application.Contracts.Persistence;

public interface IGuaranteeHistoryRepository
{
    Task<Guarantee?> GetGuaranteeWithEventsAsync(string guaranteeNumber, CancellationToken cancellationToken = default);
}

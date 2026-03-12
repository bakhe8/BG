using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;

namespace BG.Application.Contracts.Persistence;

public interface IIntakeRepository
{
    Task<IReadOnlyList<User>> ListIntakeActorsAsync(CancellationToken cancellationToken = default);

    Task<User?> GetIntakeActorByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> GuaranteeNumberExistsAsync(string guaranteeNumber, CancellationToken cancellationToken = default);

    Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default);

    Task AddGuaranteeAsync(Guarantee guarantee, CancellationToken cancellationToken = default);

    Task AddOperationsReviewItemAsync(OperationsReviewItem reviewItem, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

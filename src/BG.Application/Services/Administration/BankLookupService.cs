using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;

namespace BG.Application.Services;

internal sealed class BankLookupService : IBankLookupService
{
    private readonly IBankRepository _bankRepository;

    public BankLookupService(IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;
    }

    public async Task<string?> GetOfficialEmailByCanonicalNameAsync(string bankName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bankName))
        {
            return null;
        }

        var bank = await _bankRepository.GetByCanonicalNameAsync(bankName, cancellationToken);

        if (bank is null || !bank.IsActive || !bank.IsEmailDispatchEnabled)
        {
            return null;
        }

        return bank.OfficialEmail;
    }
}

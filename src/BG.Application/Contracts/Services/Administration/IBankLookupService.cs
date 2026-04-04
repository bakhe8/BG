namespace BG.Application.Contracts.Services;

public interface IBankLookupService
{
    Task<string?> GetOfficialEmailByCanonicalNameAsync(string bankName, CancellationToken cancellationToken = default);
}

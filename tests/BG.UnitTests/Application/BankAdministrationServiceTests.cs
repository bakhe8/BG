using BG.Application.Administration;
using BG.Application.Contracts.Persistence;
using BG.Application.Models.Administration;
using BG.Application.Services;
using BG.Domain.Guarantees;
using Microsoft.Extensions.Logging.Abstractions;

namespace BG.UnitTests.Application;

public sealed class BankAdministrationServiceTests
{
    [Fact]
    public async Task CreateBankAsync_returns_failure_for_duplicate_short_code()
    {
        var repository = new FakeBankRepository();
        repository.Banks.Add(new Bank(
            "National Bank",
            "NAT",
            null,
            isEmailDispatchEnabled: false,
            [GuaranteeDispatchChannel.Courier],
            notes: null,
            DateTimeOffset.UtcNow));

        var service = new BankAdministrationService(repository, NullLogger<BankAdministrationService>.Instance);

        var result = await service.CreateBankAsync(new CreateBankCommand(
            "National Bank Two",
            "nat",
            null,
            IsEmailDispatchEnabled: false,
            [GuaranteeDispatchChannel.Courier],
            null));

        Assert.False(result.Succeeded);
        Assert.Equal(BankAdministrationErrorCodes.DuplicateShortCode, result.ErrorCode);
    }

    [Fact]
    public async Task DeactivateBankAsync_sets_bank_as_inactive()
    {
        var repository = new FakeBankRepository();
        var bank = new Bank(
            "Alpha Bank",
            "ALP",
            null,
            isEmailDispatchEnabled: false,
            [GuaranteeDispatchChannel.HandDelivery],
            notes: null,
            DateTimeOffset.UtcNow);
        repository.Banks.Add(bank);

        var service = new BankAdministrationService(repository, NullLogger<BankAdministrationService>.Instance);

        var result = await service.DeactivateBankAsync(bank.Id);

        Assert.True(result.Succeeded);
        Assert.False(bank.IsActive);
    }

    [Fact]
    public async Task UpdateBankAsync_updates_official_email()
    {
        var repository = new FakeBankRepository();
        var bank = new Bank(
            "Beta Bank",
            "BET",
            null,
            isEmailDispatchEnabled: false,
            [GuaranteeDispatchChannel.Courier],
            notes: null,
            DateTimeOffset.UtcNow);
        repository.Banks.Add(bank);

        var service = new BankAdministrationService(repository, NullLogger<BankAdministrationService>.Instance);

        var result = await service.UpdateBankAsync(new UpdateBankCommand(
            bank.Id,
            bank.CanonicalName,
            bank.ShortCode,
            "official@beta.example",
            IsEmailDispatchEnabled: true,
            [GuaranteeDispatchChannel.OfficialEmail, GuaranteeDispatchChannel.Courier],
            bank.Notes));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("official@beta.example", result.Value.OfficialEmail);
        Assert.True(bank.IsEmailDispatchEnabled);
    }

    private sealed class FakeBankRepository : IBankRepository
    {
        public List<Bank> Banks { get; } = [];

        public Task<IReadOnlyList<Bank>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Bank>>(Banks);
        }

        public Task<Bank?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Banks.SingleOrDefault(bank => bank.Id == id));
        }

        public Task<Bank?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            var normalized = Bank.NormalizeShortCodeKey(shortCode);
            return Task.FromResult(Banks.SingleOrDefault(bank => bank.ShortCode == normalized));
        }

        public Task<Bank?> GetByCanonicalNameAsync(string canonicalName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Banks.SingleOrDefault(
                bank => string.Equals(bank.CanonicalName, canonicalName, StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(Bank bank, CancellationToken cancellationToken = default)
        {
            Banks.Add(bank);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

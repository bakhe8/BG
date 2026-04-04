using BG.Application.Administration;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Models.Administration;
using BG.Domain.Guarantees;
using Microsoft.Extensions.Logging;

namespace BG.Application.Services;

internal sealed class BankAdministrationService : IBankAdministrationService
{
    private readonly IBankRepository _repository;
    private readonly ILogger<BankAdministrationService> _logger;

    public BankAdministrationService(
        IBankRepository repository,
        ILogger<BankAdministrationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BankSummaryDto>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        var banks = await _repository.ListAllAsync(cancellationToken);

        return banks
            .OrderByDescending(bank => bank.IsActive)
            .ThenBy(bank => bank.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .Select(MapBank)
            .ToArray();
    }

    public async Task<OperationResult<BankSummaryDto>> CreateBankAsync(
        CreateBankCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CanonicalName))
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.CanonicalNameRequired);
        }

        if (string.IsNullOrWhiteSpace(command.ShortCode))
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.ShortCodeRequired);
        }

        if (command.SupportedDispatchChannels.Count == 0)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.SupportedDispatchChannelsRequired);
        }

        var existing = await _repository.GetByShortCodeAsync(command.ShortCode, cancellationToken);
        if (existing is not null)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.DuplicateShortCode);
        }

        Bank bank;
        try
        {
            bank = new Bank(
                command.CanonicalName,
                command.ShortCode,
                command.OfficialEmail,
                command.IsEmailDispatchEnabled,
                command.SupportedDispatchChannels,
                command.Notes,
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<BankSummaryDto>.Failure(MapValidationError(exception));
        }
        catch (FormatException)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.InvalidOfficialEmail);
        }

        await _repository.AddAsync(bank, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bank registry audit: bank created. BankId={BankId} ShortCode={ShortCode}",
            bank.Id,
            bank.ShortCode);

        return OperationResult<BankSummaryDto>.Success(MapBank(bank));
    }

    public async Task<OperationResult<BankSummaryDto>> UpdateBankAsync(
        UpdateBankCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.BankId == Guid.Empty)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.BankNotFound);
        }

        if (string.IsNullOrWhiteSpace(command.CanonicalName))
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.CanonicalNameRequired);
        }

        if (string.IsNullOrWhiteSpace(command.ShortCode))
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.ShortCodeRequired);
        }

        if (command.SupportedDispatchChannels.Count == 0)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.SupportedDispatchChannelsRequired);
        }

        var bank = await _repository.GetByIdAsync(command.BankId, cancellationToken);
        if (bank is null)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.BankNotFound);
        }

        var duplicated = await _repository.GetByShortCodeAsync(command.ShortCode, cancellationToken);
        if (duplicated is not null && duplicated.Id != bank.Id)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.DuplicateShortCode);
        }

        try
        {
            bank.Update(
                command.CanonicalName,
                command.ShortCode,
                command.OfficialEmail,
                command.IsEmailDispatchEnabled,
                command.SupportedDispatchChannels,
                command.Notes);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<BankSummaryDto>.Failure(MapValidationError(exception));
        }
        catch (FormatException)
        {
            return OperationResult<BankSummaryDto>.Failure(BankAdministrationErrorCodes.InvalidOfficialEmail);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bank registry audit: bank updated. BankId={BankId} ShortCode={ShortCode}",
            bank.Id,
            bank.ShortCode);

        return OperationResult<BankSummaryDto>.Success(MapBank(bank));
    }

    public async Task<OperationResult<Guid>> DeactivateBankAsync(Guid bankId, CancellationToken cancellationToken = default)
    {
        if (bankId == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(BankAdministrationErrorCodes.BankNotFound);
        }

        var bank = await _repository.GetByIdAsync(bankId, cancellationToken);
        if (bank is null)
        {
            return OperationResult<Guid>.Failure(BankAdministrationErrorCodes.BankNotFound);
        }

        bank.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bank registry audit: bank deactivated. BankId={BankId} ShortCode={ShortCode}",
            bank.Id,
            bank.ShortCode);

        return OperationResult<Guid>.Success(bank.Id);
    }

    private static BankSummaryDto MapBank(Bank bank)
    {
        return new BankSummaryDto(
            bank.Id,
            bank.CanonicalName,
            bank.ShortCode,
            bank.OfficialEmail,
            bank.IsEmailDispatchEnabled,
            bank.SupportedDispatchChannels.ToArray(),
            bank.Notes,
            bank.IsActive,
            bank.CreatedAtUtc);
    }

    private static string MapValidationError(ArgumentException exception)
    {
        if (exception.ParamName is nameof(CreateBankCommand.CanonicalName) or nameof(UpdateBankCommand.CanonicalName))
        {
            return BankAdministrationErrorCodes.CanonicalNameRequired;
        }

        if (exception.ParamName is nameof(CreateBankCommand.ShortCode) or nameof(UpdateBankCommand.ShortCode))
        {
            return BankAdministrationErrorCodes.ShortCodeRequired;
        }

        if (exception.Message.Contains("Official email is required", StringComparison.OrdinalIgnoreCase))
        {
            return BankAdministrationErrorCodes.OfficialEmailRequiredWhenEnabled;
        }

        return BankAdministrationErrorCodes.InvalidOfficialEmail;
    }
}

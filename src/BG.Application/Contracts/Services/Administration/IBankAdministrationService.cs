using BG.Application.Common;
using BG.Application.Models.Administration;

namespace BG.Application.Contracts.Services;

public interface IBankAdministrationService
{
    Task<IReadOnlyList<BankSummaryDto>> GetBanksAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<BankSummaryDto>> CreateBankAsync(CreateBankCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<BankSummaryDto>> UpdateBankAsync(UpdateBankCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> DeactivateBankAsync(Guid bankId, CancellationToken cancellationToken = default);
}

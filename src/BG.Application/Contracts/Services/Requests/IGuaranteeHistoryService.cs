using BG.Application.Common;
using BG.Application.Models.Requests;

namespace BG.Application.Contracts.Services;

public interface IGuaranteeHistoryService
{
    Task<OperationResult<PagedResult<GuaranteeEventEntryDto>>> GetGuaranteeHistoryAsync(
        string guaranteeNumber,
        Guid requestingUserId,
        string[] requestingUserPermissions,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

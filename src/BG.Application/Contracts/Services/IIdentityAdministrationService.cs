using BG.Application.Common;
using BG.Application.Models.Identity;

namespace BG.Application.Contracts.Services;

public interface IIdentityAdministrationService
{
    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<UserSummaryDto>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<UserSummaryDto>> SetUserPasswordAsync(SetLocalUserPasswordCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<RoleSummaryDto>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default);
}

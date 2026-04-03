using BG.Application.Common;
using BG.Application.Models.Identity;

namespace BG.Application.Contracts.Services;

public interface IUserAccessProfileService
{
    Task<IReadOnlyList<WorkspaceUserOptionDto>> ListActiveUsersAsync(CancellationToken cancellationToken = default);

    Task<UserAccessProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<OperationResult<UserAccessProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileCommand command, CancellationToken cancellationToken = default);
}

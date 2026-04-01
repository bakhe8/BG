using BG.Application.Models.Identity;

namespace BG.Application.Contracts.Services;

public interface IUserAccessProfileService
{
    Task<IReadOnlyList<WorkspaceUserOptionDto>> ListActiveUsersAsync(CancellationToken cancellationToken = default);

    Task<UserAccessProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}

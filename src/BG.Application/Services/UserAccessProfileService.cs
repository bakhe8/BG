using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Identity;
using BG.Application.Models.Identity;

namespace BG.Application.Services;

internal sealed class UserAccessProfileService : IUserAccessProfileService
{
    private readonly IIdentityAdministrationRepository _repository;

    public UserAccessProfileService(IIdentityAdministrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<WorkspaceUserOptionDto>> ListActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.ListUsersAsync(cancellationToken);

        return users
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(user => new WorkspaceUserOptionDto(user.Id, user.Username, user.DisplayName))
            .ToArray();
    }

    public async Task<UserAccessProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return UserAccessProfileMapper.Map(user);
    }
}

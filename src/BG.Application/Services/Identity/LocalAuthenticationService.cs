using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Identity;
using BG.Application.Models.Identity;
using BG.Domain.Identity;

namespace BG.Application.Services;

internal sealed class LocalAuthenticationService : ILocalAuthenticationService
{
    private readonly IIdentityAdministrationRepository _repository;
    private readonly ILocalPasswordHasher _passwordHasher;

    public LocalAuthenticationService(
        IIdentityAdministrationRepository repository,
        ILocalPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<OperationResult<UserAccessProfileDto>> AuthenticateAsync(
        AuthenticateLocalUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
        {
            return OperationResult<UserAccessProfileDto>.Failure(IdentityErrorCodes.InvalidCredentials);
        }

        var normalizedUsername = User.NormalizeUsernameKey(command.Username);
        var user = await _repository.GetUserByNormalizedUsernameAsync(normalizedUsername, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.SourceType != UserSourceType.Local ||
            !user.HasLocalPassword ||
            !_passwordHasher.VerifyPassword(user.PasswordHash!, command.Password))
        {
            return OperationResult<UserAccessProfileDto>.Failure(IdentityErrorCodes.InvalidCredentials);
        }

        return OperationResult<UserAccessProfileDto>.Success(UserAccessProfileMapper.Map(user));
    }
}

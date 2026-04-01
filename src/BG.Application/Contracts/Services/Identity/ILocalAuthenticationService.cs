using BG.Application.Common;
using BG.Application.Models.Identity;

namespace BG.Application.Contracts.Services;

public interface ILocalAuthenticationService
{
    Task<OperationResult<UserAccessProfileDto>> AuthenticateAsync(
        AuthenticateLocalUserCommand command,
        CancellationToken cancellationToken = default);
}

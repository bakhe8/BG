using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Identity;
using BG.Application.Models.Identity;
using BG.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BG.Application.Services;

internal sealed class IdentityAdministrationService : IIdentityAdministrationService
{
    private const int MinimumPasswordLength = 12;
    private readonly IIdentityAdministrationRepository _repository;
    private readonly ILocalPasswordHasher _passwordHasher;
    private readonly IExecutionActorAccessor _executionActorAccessor;
    private readonly ILogger<IdentityAdministrationService> _logger;

    public IdentityAdministrationService(
        IIdentityAdministrationRepository repository,
        ILocalPasswordHasher passwordHasher,
        IExecutionActorAccessor executionActorAccessor,
        ILogger<IdentityAdministrationService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _executionActorAccessor = executionActorAccessor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.ListUsersAsync(cancellationToken);

        return users
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(MapUser)
            .ToArray();
    }

    public async Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _repository.ListRolesAsync(cancellationToken);

        return roles
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapRole)
            .ToArray();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _repository.ListPermissionsAsync(cancellationToken);

        return permissions
            .OrderBy(permission => permission.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(permission => permission.Key, StringComparer.OrdinalIgnoreCase)
            .Select(permission => new PermissionDto(permission.Key, permission.Area))
            .ToArray();
    }

    public async Task<OperationResult<UserSummaryDto>> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username))
        {
            return OperationResult<UserSummaryDto>.Failure(IdentityErrorCodes.UsernameRequired);
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return OperationResult<UserSummaryDto>.Failure(IdentityErrorCodes.DisplayNameRequired);
        }

        var passwordError = ValidatePassword(command.InitialPassword);
        if (passwordError is not null)
        {
            return OperationResult<UserSummaryDto>.Failure(passwordError);
        }

        var normalizedUsername = User.NormalizeUsernameKey(command.Username);

        if (await _repository.UsernameExistsAsync(normalizedUsername, cancellationToken))
        {
            return OperationResult<UserSummaryDto>.Failure(IdentityErrorCodes.DuplicateUsername);
        }

        var roleIds = command.RoleIds
            .Where(roleId => roleId != Guid.Empty)
            .Distinct()
            .ToArray();

        var roles = await _repository.GetRolesByIdsAsync(roleIds, cancellationToken);

        if (roles.Count != roleIds.Length)
        {
            return OperationResult<UserSummaryDto>.Failure(IdentityErrorCodes.RoleNotFound);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User(
            command.Username,
            command.DisplayName,
            command.Email,
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: now);

        user.SetLocalPassword(_passwordHasher.HashPassword(command.InitialPassword), now);
        user.AssignRoles(roles);

        await _repository.AddUserAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        AuditUserCreated(user, roles);

        return OperationResult<UserSummaryDto>.Success(MapUser(user));
    }

    public async Task<OperationResult<UserSummaryDto>> SetUserPasswordAsync(
        SetLocalUserPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            return OperationResult<UserSummaryDto>.Failure(IdentityErrorCodes.UserNotFound);
        }

        var passwordError = ValidatePassword(command.NewPassword);
        if (passwordError is not null)
        {
            return OperationResult<UserSummaryDto>.Failure(passwordError);
        }

        var user = await _repository.GetUserByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummaryDto>.Failure(IdentityErrorCodes.UserNotFound);
        }

        user.SetLocalPassword(_passwordHasher.HashPassword(command.NewPassword), DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        AuditPasswordChanged(user);

        return OperationResult<UserSummaryDto>.Success(MapUser(user));
    }

    public async Task<OperationResult<RoleSummaryDto>> CreateRoleAsync(
        CreateRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return OperationResult<RoleSummaryDto>.Failure(IdentityErrorCodes.RoleNameRequired);
        }

        var normalizedRoleName = Role.NormalizeNameKey(command.Name);

        if (await _repository.RoleNameExistsAsync(normalizedRoleName, cancellationToken))
        {
            return OperationResult<RoleSummaryDto>.Failure(IdentityErrorCodes.DuplicateRoleName);
        }

        var permissionKeys = command.PermissionKeys
            .Where(permissionKey => !string.IsNullOrWhiteSpace(permissionKey))
            .Select(permissionKey => permissionKey.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await _repository.GetPermissionsByKeysAsync(permissionKeys, cancellationToken);

        if (permissions.Count != permissionKeys.Length)
        {
            return OperationResult<RoleSummaryDto>.Failure(IdentityErrorCodes.PermissionNotFound);
        }

        var role = new Role(command.Name, command.Description);
        role.AssignPermissions(permissions);

        await _repository.AddRoleAsync(role, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        AuditRoleCreated(role, permissionKeys);

        return OperationResult<RoleSummaryDto>.Success(MapRole(role));
    }

    private void AuditUserCreated(User user, IReadOnlyList<Role> roles)
    {
        var actor = _executionActorAccessor.GetCurrentActor();
        var assignedRoles = roles
            .Select(role => role.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation(
            "Identity audit: local user created and roles assigned. ActorUserId={ActorUserId} ActorUsername={ActorUsername} ActorDisplayName={ActorDisplayName} TargetUserId={TargetUserId} TargetUsername={TargetUsername} AssignedRoles={AssignedRoles}",
            actor?.UserId,
            actor?.Username,
            actor?.DisplayName,
            user.Id,
            user.Username,
            assignedRoles);
    }

    private void AuditPasswordChanged(User user)
    {
        var actor = _executionActorAccessor.GetCurrentActor();

        _logger.LogInformation(
            "Identity audit: local password changed. ActorUserId={ActorUserId} ActorUsername={ActorUsername} ActorDisplayName={ActorDisplayName} TargetUserId={TargetUserId} TargetUsername={TargetUsername}",
            actor?.UserId,
            actor?.Username,
            actor?.DisplayName,
            user.Id,
            user.Username);
    }

    private void AuditRoleCreated(Role role, IReadOnlyList<string> permissionKeys)
    {
        var actor = _executionActorAccessor.GetCurrentActor();

        _logger.LogInformation(
            "Identity audit: role created. ActorUserId={ActorUserId} ActorUsername={ActorUsername} ActorDisplayName={ActorDisplayName} RoleId={RoleId} RoleName={RoleName} PermissionKeys={PermissionKeys}",
            actor?.UserId,
            actor?.Username,
            actor?.DisplayName,
            role.Id,
            role.Name,
            permissionKeys);
    }

    private static UserSummaryDto MapUser(User user)
    {
        return new UserSummaryDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.ExternalId,
            user.SourceType.ToString(),
            user.IsActive,
            user.HasLocalPassword,
            user.PasswordChangedAtUtc,
            user.CreatedAtUtc,
            user.UserRoles
                .Select(userRole => userRole.Role.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static RoleSummaryDto MapRole(Role role)
    {
        return new RoleSummaryDto(
            role.Id,
            role.Name,
            role.Description,
            role.RolePermissions
                .Select(rolePermission => rolePermission.Permission.Key)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return IdentityErrorCodes.PasswordRequired;
        }

        return password.Length < MinimumPasswordLength
            ? IdentityErrorCodes.PasswordTooShort
            : null;
    }
}

using BG.Application.Approvals;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Models.Approvals;
using BG.Domain.Workflow;

namespace BG.Application.Services;

internal sealed class ApprovalDelegationAdministrationService : IApprovalDelegationAdministrationService
{
    private readonly IIdentityAdministrationRepository _repository;

    public ApprovalDelegationAdministrationService(IIdentityAdministrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApprovalDelegationAdministrationSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.ListUsersAsync(cancellationToken);
        var roles = await _repository.ListRolesAsync(cancellationToken);
        var delegations = await _repository.ListApprovalDelegationsAsync(cancellationToken);
        var effectiveAtUtc = DateTimeOffset.UtcNow;

        return new ApprovalDelegationAdministrationSnapshotDto(
            delegations
                .OrderByDescending(delegation => delegation.StartsAtUtc)
                .Select(delegation => new ApprovalDelegationSummaryDto(
                    delegation.Id,
                    delegation.DelegatorUserId,
                    delegation.DelegatorUser.DisplayName,
                    delegation.DelegateUserId,
                    delegation.DelegateUser.DisplayName,
                    delegation.RoleId,
                    delegation.Role.Name,
                    delegation.StartsAtUtc,
                    delegation.EndsAtUtc,
                    delegation.Reason,
                    delegation.CreatedAtUtc,
                    ApprovalDelegationStatusCatalog.GetResourceKey(delegation, effectiveAtUtc),
                    delegation.RevokedAtUtc,
                    delegation.RevocationReason))
                .ToArray(),
            users
                .Where(user => user.IsActive)
                .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(user => new ApprovalDelegationUserOptionDto(user.Id, user.Username, user.DisplayName))
                .ToArray(),
            roles
                .Where(IsApprovalRole)
                .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
                .Select(role => new ApprovalDelegationRoleOptionDto(role.Id, role.Name, role.Description))
                .ToArray());
    }

    public async Task<OperationResult<Guid>> CreateAsync(
        CreateApprovalDelegationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DelegatorUserId == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.DelegatorRequired);
        }

        if (command.DelegateUserId == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.DelegateRequired);
        }

        if (command.RoleId == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.RoleRequired);
        }

        if (command.DelegatorUserId == command.DelegateUserId)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.SameUserNotAllowed);
        }

        if (command.EndsAtUtc <= command.StartsAtUtc)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.InvalidPeriod);
        }

        var delegator = await _repository.GetUserByIdAsync(command.DelegatorUserId, cancellationToken);
        var delegateUser = await _repository.GetUserByIdAsync(command.DelegateUserId, cancellationToken);

        if (delegator is null || !delegator.IsActive || delegateUser is null || !delegateUser.IsActive)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.UserNotFound);
        }

        var role = (await _repository.GetRolesByIdsAsync([command.RoleId], cancellationToken)).SingleOrDefault();
        if (role is null)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.RoleNotFound);
        }

        if (!IsApprovalRole(role))
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.RoleNotFound);
        }

        if (delegator.UserRoles.All(userRole => userRole.RoleId != role.Id))
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.DelegatorRoleRequired);
        }

        if (await _repository.HasOverlappingApprovalDelegationAsync(
                command.DelegateUserId,
                command.RoleId,
                command.StartsAtUtc,
                command.EndsAtUtc,
                cancellationToken))
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.Overlap);
        }

        var delegation = new ApprovalDelegation(
            command.DelegatorUserId,
            command.DelegateUserId,
            command.RoleId,
            command.StartsAtUtc,
            command.EndsAtUtc,
            command.Reason,
            DateTimeOffset.UtcNow);

        await _repository.AddApprovalDelegationAsync(delegation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(delegation.Id);
    }

    public async Task<OperationResult<Guid>> RevokeAsync(
        RevokeApprovalDelegationCommand command,
        CancellationToken cancellationToken = default)
    {
        var delegation = await _repository.GetApprovalDelegationByIdAsync(command.DelegationId, cancellationToken);
        if (delegation is null)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.DelegationNotFound);
        }

        if (delegation.RevokedAtUtc.HasValue)
        {
            return OperationResult<Guid>.Failure(ApprovalDelegationErrorCodes.DelegationAlreadyRevoked);
        }

        delegation.Revoke(DateTimeOffset.UtcNow, command.Reason);
        await _repository.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(delegation.Id);
    }

    private static bool IsApprovalRole(BG.Domain.Identity.Role role)
    {
        return role.RolePermissions.Any(rolePermission =>
            rolePermission.PermissionKey == "approvals.queue.view" ||
            rolePermission.PermissionKey == "approvals.sign");
    }
}

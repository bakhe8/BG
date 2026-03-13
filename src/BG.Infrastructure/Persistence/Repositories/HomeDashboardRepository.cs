using BG.Application.Contracts.Persistence;
using BG.Application.Models.Dashboard;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class HomeDashboardRepository : IHomeDashboardRepository
{
    private const int PreviewLimit = 5;
    private readonly BgDbContext _dbContext;

    public HomeDashboardRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HomeDashboardSnapshotDto> GetAuthenticatedDashboardAsync(
        HomeDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var actionableApprovalRoleIds = query.IncludeApprovals
            ? await ListActionableApprovalRoleIdsAsync(query.UserId, query.NowUtc, cancellationToken)
            : [];

        var pendingApprovalsCount = actionableApprovalRoleIds.Length > 0
            ? await CountPendingApprovalsAsync(actionableApprovalRoleIds, cancellationToken)
            : 0;
        var pendingApprovals = actionableApprovalRoleIds.Length > 0
            ? await ListPendingApprovalsAsync(actionableApprovalRoleIds, cancellationToken)
            : [];

        var myOpenRequestsCount = query.IncludeRequests
            ? await CountOpenRequestsAsync(query.UserId, cancellationToken)
            : 0;
        var openRequests = query.IncludeRequests
            ? await ListOpenRequestsAsync(query.UserId, cancellationToken)
            : [];

        var operationsBacklogCount = query.IncludeOperations
            ? await _dbContext.OperationsReviewItems
                .AsNoTracking()
                .CountAsync(item => item.Status != BG.Domain.Operations.OperationsReviewItemStatus.Completed, cancellationToken)
            : 0;

        var readyForDispatchCount = query.IncludeDispatch
            ? await _dbContext.GuaranteeRequests
                .AsNoTracking()
                .CountAsync(
                    request => request.Status == GuaranteeRequestStatus.ApprovedForDispatch,
                    cancellationToken)
            : 0;

        var pendingDeliveryCount = query.IncludeDispatch
            ? await _dbContext.GuaranteeCorrespondence
                .AsNoTracking()
                .CountAsync(
                    correspondence =>
                        correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                        correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter &&
                        correspondence.DispatchedAtUtc.HasValue &&
                        !correspondence.DeliveredAtUtc.HasValue,
                    cancellationToken)
            : 0;

        var expiringGuaranteesCount = query.IncludeExpiringGuarantees
            ? await BuildExpiringGuaranteesQuery(query.Today)
                .CountAsync(cancellationToken)
            : 0;
        var expiringGuarantees = query.IncludeExpiringGuarantees
            ? await ListExpiringGuaranteesAsync(query.Today, cancellationToken)
            : [];

        var recentIntakeActivities = query.IncludeIntake
            ? await ListRecentIntakeActivitiesAsync(cancellationToken)
            : [];

        return new HomeDashboardSnapshotDto(
            true,
            null,
            query.IncludeApprovals,
            query.IncludeRequests,
            query.IncludeOperations,
            query.IncludeDispatch,
            query.IncludeIntake,
            query.IncludeExpiringGuarantees,
            pendingApprovalsCount,
            pendingApprovals,
            myOpenRequestsCount,
            openRequests,
            operationsBacklogCount,
            readyForDispatchCount,
            pendingDeliveryCount,
            expiringGuaranteesCount,
            expiringGuarantees,
            recentIntakeActivities);
    }

    private async Task<Guid[]> ListActionableApprovalRoleIdsAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var directRoleIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId == userId &&
                userRole.User.IsActive &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.PermissionKey == "approvals.queue.view" ||
                    rolePermission.PermissionKey == "approvals.sign"))
            .Select(userRole => userRole.RoleId)
            .ToListAsync(cancellationToken);

        IReadOnlyList<DelegatedRoleRef> delegatedRoleIds;
        if (RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            delegatedRoleIds = (await _dbContext.ApprovalDelegations
                    .AsNoTracking()
                    .ToListAsync(cancellationToken))
                .Where(delegation =>
                    delegation.DelegateUserId == userId &&
                    !delegation.RevokedAtUtc.HasValue &&
                    delegation.StartsAtUtc <= nowUtc &&
                    delegation.EndsAtUtc >= nowUtc)
                .Select(delegation => new DelegatedRoleRef(delegation.RoleId, delegation.DelegatorUserId))
                .ToArray();
        }
        else
        {
            delegatedRoleIds = await BuildActiveApprovalDelegationsQuery(nowUtc)
                .Where(delegation => delegation.DelegateUserId == userId)
                .Select(delegation => new DelegatedRoleRef(delegation.RoleId, delegation.DelegatorUserId))
                .ToListAsync(cancellationToken);
        }

        if (delegatedRoleIds.Count == 0)
        {
            return directRoleIds
                .Distinct()
                .ToArray();
        }

        var validDelegatorRolePairs = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                delegatedRoleIds.Select(item => item.DelegatorUserId).Contains(userRole.UserId) &&
                delegatedRoleIds.Select(item => item.RoleId).Contains(userRole.RoleId) &&
                userRole.User.IsActive)
            .Select(userRole => new { userRole.UserId, userRole.RoleId })
            .ToListAsync(cancellationToken);

        return directRoleIds
            .Concat(
                delegatedRoleIds
                    .Where(item => validDelegatorRolePairs.Any(pair => pair.UserId == item.DelegatorUserId && pair.RoleId == item.RoleId))
                    .Select(item => item.RoleId))
            .Distinct()
            .ToArray();
    }

    private Task<int> CountPendingApprovalsAsync(Guid[] actionableRoleIds, CancellationToken cancellationToken)
    {
        return BuildPendingApprovalsQuery(actionableRoleIds).CountAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<HomeDashboardApprovalItemDto>> ListPendingApprovalsAsync(
        Guid[] actionableRoleIds,
        CancellationToken cancellationToken)
    {
        PendingApprovalStageRef[] pendingStageRefs;
        if (RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            pendingStageRefs = (await BuildPendingApprovalsQuery(actionableRoleIds)
                    .Select(stage => new PendingApprovalStageRef(
                        stage.ApprovalProcess.GuaranteeRequestId,
                        stage.ApprovalProcess.SubmittedAtUtc,
                        stage.TitleResourceKey,
                        stage.TitleText,
                        stage.RoleId))
                    .ToListAsync(cancellationToken))
                .OrderBy(stage => stage.SubmittedAtUtc)
                .Take(PreviewLimit)
                .ToArray();
        }
        else
        {
            pendingStageRefs = await BuildPendingApprovalsQuery(actionableRoleIds)
                .OrderBy(stage => stage.ApprovalProcess.SubmittedAtUtc)
                .Take(PreviewLimit)
                .Select(stage => new PendingApprovalStageRef(
                    stage.ApprovalProcess.GuaranteeRequestId,
                    stage.ApprovalProcess.SubmittedAtUtc,
                    stage.TitleResourceKey,
                    stage.TitleText,
                    stage.RoleId))
                .ToArrayAsync(cancellationToken);
        }

        if (pendingStageRefs.Length == 0)
        {
            return [];
        }

        var requestIds = pendingStageRefs
            .Select(item => item.RequestId)
            .Distinct()
            .ToArray();
        var roleIds = pendingStageRefs
            .Where(item => item.RoleId.HasValue)
            .Select(item => item.RoleId!.Value)
            .Distinct()
            .ToArray();

        var requestDetails = await _dbContext.GuaranteeRequests
            .AsNoTracking()
            .Where(request => requestIds.Contains(request.Id))
            .Select(request => new PendingApprovalRequestRef(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                request.RequestType))
            .ToArrayAsync(cancellationToken);

        var roleLookup = roleIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Roles
                .AsNoTracking()
                .Where(role => roleIds.Contains(role.Id))
                .ToDictionaryAsync(role => role.Id, role => role.Name, cancellationToken);

        var requestLookup = requestDetails.ToDictionary(request => request.RequestId);

        return pendingStageRefs
            .Where(item => requestLookup.ContainsKey(item.RequestId))
            .Select(item =>
            {
                var request = requestLookup[item.RequestId];
                return new HomeDashboardApprovalItemDto(
                    item.RequestId,
                    request.GuaranteeNumber,
                    request.RequestType,
                    item.SubmittedAtUtc,
                    item.StageTitleResourceKey,
                    item.StageTitleText,
                    item.RoleId.HasValue && roleLookup.TryGetValue(item.RoleId.Value, out var roleName)
                        ? roleName
                        : null);
            })
            .ToArray();
    }

    private Task<int> CountOpenRequestsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return BuildOpenRequestsQuery(userId).CountAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<HomeDashboardRequestItemDto>> ListOpenRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            return (await BuildOpenRequestsQuery(userId)
                    .Select(request => new HomeDashboardRequestItemDto(
                        request.Id,
                        request.Guarantee.GuaranteeNumber,
                        request.RequestType,
                        request.Status,
                        request.CreatedAtUtc))
                    .ToListAsync(cancellationToken))
                .OrderByDescending(request => request.CreatedAtUtc)
                .Take(PreviewLimit)
                .ToArray();
        }

        return await BuildOpenRequestsQuery(userId)
            .OrderByDescending(request => request.CreatedAtUtc)
            .Take(PreviewLimit)
            .Select(request => new HomeDashboardRequestItemDto(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                request.RequestType,
                request.Status,
                request.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<Guarantee> BuildExpiringGuaranteesQuery(DateOnly today)
    {
        var threshold = today.AddDays(30);

        return _dbContext.Guarantees
            .AsNoTracking()
            .Where(guarantee =>
                guarantee.Status == GuaranteeStatus.Active &&
                guarantee.ExpiryDate >= today &&
                guarantee.ExpiryDate <= threshold);
    }

    private async Task<IReadOnlyList<HomeDashboardExpiringGuaranteeDto>> ListExpiringGuaranteesAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var items = await BuildExpiringGuaranteesQuery(today)
            .OrderBy(guarantee => guarantee.ExpiryDate)
            .ThenBy(guarantee => guarantee.GuaranteeNumber)
            .Take(PreviewLimit)
            .Select(guarantee => new HomeDashboardExpiringGuaranteeDto(
                guarantee.Id,
                guarantee.GuaranteeNumber,
                guarantee.Category,
                guarantee.CurrentAmount,
                guarantee.CurrencyCode,
                guarantee.ExpiryDate,
                0))
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item => item with
            {
                DaysRemaining = item.ExpiryDate.DayNumber - today.DayNumber
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<HomeDashboardIntakeActivityDto>> ListRecentIntakeActivitiesAsync(
        CancellationToken cancellationToken)
    {
        if (RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            return (await _dbContext.GuaranteeDocuments
                    .AsNoTracking()
                    .Select(document => new HomeDashboardIntakeActivityDto(
                        document.Id,
                        document.Guarantee.GuaranteeNumber,
                        document.FileName,
                        document.DocumentType,
                        document.CapturedAtUtc,
                        document.CapturedByDisplayName,
                        document.CaptureChannel,
                        document.IntakeScenarioKey,
                        "OperationsReviewScenario_Unknown"))
                    .ToListAsync(cancellationToken))
                .OrderByDescending(document => document.CapturedAtUtc)
                .Take(PreviewLimit)
                .ToArray();
        }

        return await _dbContext.GuaranteeDocuments
            .AsNoTracking()
            .OrderByDescending(document => document.CapturedAtUtc)
            .Take(PreviewLimit)
            .Select(document => new HomeDashboardIntakeActivityDto(
                document.Id,
                document.Guarantee.GuaranteeNumber,
                document.FileName,
                document.DocumentType,
                document.CapturedAtUtc,
                document.CapturedByDisplayName,
                document.CaptureChannel,
                document.IntakeScenarioKey,
                "OperationsReviewScenario_Unknown"))
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<RequestApprovalStage> BuildPendingApprovalsQuery(Guid[] actionableRoleIds)
    {
        return _dbContext.RequestApprovalStages
            .AsNoTracking()
            .Where(stage =>
                stage.Status == RequestApprovalStageStatus.Active &&
                stage.RoleId.HasValue &&
                actionableRoleIds.Contains(stage.RoleId.Value) &&
                stage.ApprovalProcess.Status == RequestApprovalProcessStatus.InProgress);
    }

    private IQueryable<GuaranteeRequest> BuildOpenRequestsQuery(Guid userId)
    {
        return _dbContext.GuaranteeRequests
            .AsNoTracking()
            .Where(request =>
                request.RequestedByUserId == userId &&
                request.Status != GuaranteeRequestStatus.Completed &&
                request.Status != GuaranteeRequestStatus.Rejected &&
                request.Status != GuaranteeRequestStatus.Cancelled);
    }

    private IQueryable<ApprovalDelegation> BuildActiveApprovalDelegationsQuery(DateTimeOffset nowUtc)
    {
        return _dbContext.ApprovalDelegations
            .AsNoTracking()
            .Where(delegation =>
                !delegation.RevokedAtUtc.HasValue &&
                delegation.StartsAtUtc <= nowUtc &&
                delegation.EndsAtUtc >= nowUtc);
    }

    private sealed record DelegatedRoleRef(Guid RoleId, Guid DelegatorUserId);

    private sealed record PendingApprovalStageRef(
        Guid RequestId,
        DateTimeOffset SubmittedAtUtc,
        string? StageTitleResourceKey,
        string? StageTitleText,
        Guid? RoleId);

    private sealed record PendingApprovalRequestRef(
        Guid RequestId,
        string GuaranteeNumber,
        GuaranteeRequestType RequestType);
}

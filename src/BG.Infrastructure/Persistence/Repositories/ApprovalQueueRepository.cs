using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Models.Approvals;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class ApprovalQueueRepository : IApprovalQueueRepository
{
    private readonly BgDbContext _dbContext;

    public ApprovalQueueRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListApprovalActorsAsync(CancellationToken cancellationToken = default)
    {
        var effectiveAtUtc = DateTimeOffset.UtcNow;
        var activeRoleIds = await _dbContext.RequestWorkflowDefinitions
            .Where(definition => definition.IsActive)
            .SelectMany(definition => definition.Stages)
            .Where(stage => stage.RoleId.HasValue)
            .Select(stage => stage.RoleId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (activeRoleIds.Count == 0)
        {
            return Array.Empty<User>();
        }

        var directActorIds = await _dbContext.Users
            .Where(user => user.IsActive &&
                           user.UserRoles.Any(userRole =>
                               activeRoleIds.Contains(userRole.RoleId) &&
                               userRole.Role.RolePermissions.Any(rolePermission =>
                                   rolePermission.PermissionKey == "approvals.queue.view" ||
                                    rolePermission.PermissionKey == "approvals.sign")))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        var delegatedActorIds = (await ListActiveDelegationsInternalAsync(effectiveAtUtc, cancellationToken))
            .Where(delegation => activeRoleIds.Contains(delegation.RoleId))
            .Select(delegation => delegation.DelegateUserId)
            .Distinct()
            .ToList();

        var actorIds = directActorIds
            .Concat(delegatedActorIds)
            .Distinct()
            .ToArray();

        if (actorIds.Length == 0)
        {
            return Array.Empty<User>();
        }

        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .Where(user => actorIds.Contains(user.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetApprovalActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var effectiveAtUtc = DateTimeOffset.UtcNow;
        var hasDirectApprovalAccess = await _dbContext.Users.AnyAsync(
            user => user.Id == userId &&
                    user.IsActive &&
                    user.UserRoles.Any(userRole =>
                        userRole.Role.RolePermissions.Any(rolePermission =>
                            rolePermission.PermissionKey == "approvals.sign")),
            cancellationToken);

        var hasDelegatedApprovalAccess = (await ListActiveDelegationsInternalAsync(effectiveAtUtc, cancellationToken))
            .Any(delegation => delegation.DelegateUserId == userId);

        if (!hasDirectApprovalAccess && !hasDelegatedApprovalAccess)
        {
            return null;
        }

        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .SingleOrDefaultAsync(
                user => user.Id == userId && user.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalDelegation>> ListActiveDelegationsAsync(
        Guid delegateUserId,
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken = default)
    {
        return (await ListActiveDelegationsInternalAsync(effectiveAtUtc, cancellationToken))
            .Where(delegation => delegation.DelegateUserId == delegateUserId)
            .ToArray();
    }

    public async Task<PagedResult<ApprovalQueueItemReadModel>> ListActionableRequestsAsync(
        IEnumerable<Guid> actionableRoleIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var roleIds = actionableRoleIds
            .Where(roleId => roleId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
        {
            return new PagedResult<ApprovalQueueItemReadModel>([], RepositoryPaging.CreatePageInfo(1, pageSize, 0));
        }

        var filteredQuery = BuildActionableRequestsFilteredQuery(roleIds);

        var totalItemCount = await filteredQuery.CountAsync(cancellationToken);
        var pageInfo = RepositoryPaging.CreatePageInfo(pageNumber, pageSize, totalItemCount);

        IReadOnlyList<ApprovalQueueItemReadModel> requests = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? RepositoryPaging.SlicePage(
                (await BuildActionableRequestsProjection(filteredQuery)
                    .ToListAsync(cancellationToken))
                    .OrderBy(request => request.SubmittedAtUtc),
                pageInfo)
            : await BuildActionableRequestsProjection(
                    filteredQuery.OrderBy(request => request.ApprovalProcess!.SubmittedAtUtc))
                .Skip((pageInfo.PageNumber - 1) * pageInfo.PageSize)
                .Take(pageInfo.PageSize)
                .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            return new PagedResult<ApprovalQueueItemReadModel>(requests, pageInfo);
        }

        var hydratedRequests = await HydrateRequestsAsync(requests, cancellationToken);

        return new PagedResult<ApprovalQueueItemReadModel>(hydratedRequests, pageInfo);
    }

    public async Task<ApprovalQueueItemReadModel?> GetActionableRequestAsync(
        Guid requestId,
        IEnumerable<Guid> actionableRoleIds,
        CancellationToken cancellationToken = default)
    {
        var roleIds = actionableRoleIds
            .Where(roleId => roleId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
        {
            return null;
        }

        var projectedQuery = BuildActionableRequestsProjection(
            BuildActionableRequestsFilteredQuery(roleIds)
                .Where(request => request.Id == requestId));

        ApprovalQueueItemReadModel? request = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? (await projectedQuery.ToListAsync(cancellationToken)).SingleOrDefault()
            : await projectedQuery.SingleOrDefaultAsync(cancellationToken);

        if (request is null)
        {
            return null;
        }

        return (await HydrateRequestsAsync([request], cancellationToken)).Single();
    }

    public async Task<GuaranteeRequest?> GetRequestForApprovalAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeRequests
            .Include(request => request.Guarantee)
            .ThenInclude(guarantee => guarantee.Events)
            .Include(request => request.RequestedByUser)
            .Include(request => request.RequestDocuments)
            .ThenInclude(link => link.GuaranteeDocument)
            .Include(request => request.ApprovalProcess!)
            .ThenInclude(process => process.Stages)
            .ThenInclude(stage => stage.Role)
            .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);
    }

    public async Task<GuaranteeDocument?> GetRequestDocumentAsync(Guid requestId, Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeRequestDocuments
            .Where(link => link.GuaranteeRequestId == requestId && link.GuaranteeDocumentId == documentId)
            .Select(link => link.GuaranteeDocument)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<GuaranteeRequest> BuildActionableRequestsFilteredQuery(Guid[] roleIds)
    {
        return _dbContext.GuaranteeRequests
            .Where(request =>
                request.ApprovalProcess != null &&
                request.ApprovalProcess.Status == RequestApprovalProcessStatus.InProgress &&
                request.ApprovalProcess.Stages.Any(stage =>
                    stage.Status == RequestApprovalStageStatus.Active &&
                    stage.RoleId.HasValue &&
                    roleIds.Contains(stage.RoleId.Value)))
            .AsNoTracking();
    }

    private static IQueryable<ApprovalQueueItemReadModel> BuildActionableRequestsProjection(IQueryable<GuaranteeRequest> filteredQuery)
    {
        return filteredQuery
            .Select(request => new ApprovalQueueItemReadModel(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                request.Guarantee.Category,
                request.RequestType,
                request.RequestChannel,
                request.Status,
                request.RequestedByUser.DisplayName,
                request.CreatedAtUtc,
                request.ApprovalProcess!.SubmittedAtUtc,
                request.Guarantee.CurrentAmount,
                request.RequestedAmount,
                request.RequestedExpiryDate,
                request.Notes,
                request.ApprovalProcess.Stages.Count,
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => (int?)stage.Sequence)
                    .FirstOrDefault(),
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => stage.RoleId)
                    .FirstOrDefault(),
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => stage.TitleResourceKey)
                    .FirstOrDefault(),
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => stage.TitleText)
                    .FirstOrDefault(),
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => stage.Role != null ? stage.Role.Name : null)
                    .FirstOrDefault(),
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => stage.RequiresLetterSignature)
                    .FirstOrDefault(),
                Array.Empty<ApprovalPriorSignatureReadModel>(),
                Array.Empty<ApprovalRequestAttachmentReadModel>(),
                Array.Empty<ApprovalRequestTimelineEntryReadModel>(),
                request.ApprovalProcess.FinalSignatureDelegationPolicy,
                request.ApprovalProcess.DelegationAmountThreshold,
                request.ApprovalProcess.Stages
                    .Where(stage => stage.Status == RequestApprovalStageStatus.Active)
                    .OrderBy(stage => stage.Sequence)
                    .Select(stage => (ApprovalDelegationPolicy?)stage.DelegationPolicy)
                    .FirstOrDefault() ?? ApprovalDelegationPolicy.Inherit))
            .AsNoTracking();
    }

    private async Task<IReadOnlyList<ApprovalQueueItemReadModel>> HydrateRequestsAsync(
        IReadOnlyList<ApprovalQueueItemReadModel> requests,
        CancellationToken cancellationToken)
    {
        var requestIds = requests.Select(request => request.RequestId).ToArray();

        var attachmentsQuery = _dbContext.GuaranteeRequestDocuments
            .AsNoTracking()
            .Where(link => requestIds.Contains(link.GuaranteeRequestId))
            .Select(link => new
            {
                link.GuaranteeRequestId,
                Attachment = new ApprovalRequestAttachmentReadModel(
                    link.Id,
                    link.GuaranteeDocumentId,
                    link.GuaranteeDocument.FileName,
                    link.GuaranteeDocument.DocumentType,
                    link.LinkedAtUtc,
                    link.LinkedByDisplayName,
                    link.GuaranteeDocument.CapturedAtUtc,
                    link.GuaranteeDocument.CapturedByDisplayName,
                    link.GuaranteeDocument.CaptureChannel,
                    link.GuaranteeDocument.SourceSystemName,
                    link.GuaranteeDocument.SourceReference,
                    link.GuaranteeDocument.VerifiedDataJson)
            });

        var attachments = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? (await attachmentsQuery.ToListAsync(cancellationToken))
                .OrderBy(entry => entry.Attachment.LinkedAtUtc)
                .ToList()
            : await attachmentsQuery
                .OrderBy(entry => entry.Attachment.LinkedAtUtc)
                .ToListAsync(cancellationToken);

        var attachmentsByRequestId = attachments
            .GroupBy(entry => entry.GuaranteeRequestId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApprovalRequestAttachmentReadModel>)group.Select(entry => entry.Attachment).ToArray());

        var priorSignaturesQuery = _dbContext.RequestApprovalStages
            .AsNoTracking()
            .Where(stage =>
                requestIds.Contains(stage.ApprovalProcess.GuaranteeRequestId) &&
                stage.Status == RequestApprovalStageStatus.Approved &&
                stage.ActedAtUtc.HasValue)
            .Select(stage => new
            {
                RequestId = stage.ApprovalProcess.GuaranteeRequestId,
                PriorSignature = new ApprovalPriorSignatureReadModel(
                    stage.Id,
                    stage.Sequence,
                    stage.TitleResourceKey,
                    stage.TitleText,
                    stage.Role != null ? stage.Role.Name : null,
                    stage.ActedAtUtc!.Value,
                    stage.ActedByUserId,
                    stage.ActedByUser != null ? stage.ActedByUser.DisplayName : null,
                    stage.ActedOnBehalfOfUserId ?? stage.ActedByUserId,
                    stage.ActedOnBehalfOfUser != null
                        ? stage.ActedOnBehalfOfUser.DisplayName
                        : stage.ActedByUser != null
                            ? stage.ActedByUser.DisplayName
                            : null)
            });

        var priorSignatures = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? (await priorSignaturesQuery.ToListAsync(cancellationToken))
                .OrderBy(entry => entry.PriorSignature.Sequence)
                .ThenBy(entry => entry.PriorSignature.ActedAtUtc)
                .ToList()
            : await priorSignaturesQuery
                .OrderBy(entry => entry.PriorSignature.Sequence)
                .ThenBy(entry => entry.PriorSignature.ActedAtUtc)
                .ToListAsync(cancellationToken);

        var priorSignaturesByRequestId = priorSignatures
            .GroupBy(entry => entry.RequestId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApprovalPriorSignatureReadModel>)group.Select(entry => entry.PriorSignature).ToArray());

        var timelineEntriesQuery = _dbContext.GuaranteeEvents
            .AsNoTracking()
            .Where(ledgerEntry => ledgerEntry.GuaranteeRequestId.HasValue && requestIds.Contains(ledgerEntry.GuaranteeRequestId.Value))
            .Select(ledgerEntry => new ApprovalRequestTimelineEntryReadModel(
                ledgerEntry.GuaranteeRequestId!.Value,
                ledgerEntry.Id,
                ledgerEntry.OccurredAtUtc,
                ledgerEntry.ActorDisplayName,
                ledgerEntry.Summary,
                ledgerEntry.ApprovalStageLabel,
                ledgerEntry.ApprovalPolicyResourceKey,
                ledgerEntry.ApprovalResponsibleSignerDisplayName,
                ledgerEntry.ApprovalExecutionMode,
                ledgerEntry.DispatchStageResourceKey,
                ledgerEntry.DispatchMethodResourceKey,
                ledgerEntry.DispatchPolicyResourceKey,
                ledgerEntry.OperationsScenarioTitleResourceKey,
                ledgerEntry.OperationsLaneResourceKey,
                ledgerEntry.OperationsMatchConfidenceResourceKey,
                ledgerEntry.OperationsMatchScore,
                ledgerEntry.OperationsPolicyResourceKey));

        var timelineEntries = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? (await timelineEntriesQuery.ToListAsync(cancellationToken))
                .OrderBy(entry => entry.OccurredAtUtc)
                .ToList()
            : await timelineEntriesQuery
                .OrderBy(entry => entry.OccurredAtUtc)
                .ToListAsync(cancellationToken);

        var timelineByRequestId = timelineEntries
            .GroupBy(entry => entry.RequestId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApprovalRequestTimelineEntryReadModel>)group.ToArray());

        var hydratedRequests = requests
            .Select(request => request with
            {
                PriorSignatures = priorSignaturesByRequestId.GetValueOrDefault(request.RequestId, []),
                Attachments = attachmentsByRequestId.GetValueOrDefault(request.RequestId, []),
                TimelineEntries = timelineByRequestId.GetValueOrDefault(request.RequestId, [])
            })
            .ToArray();

        return hydratedRequests;
    }

    private IQueryable<ApprovalDelegation> BuildActiveDelegationsQuery(DateTimeOffset effectiveAtUtc)
    {
        return _dbContext.ApprovalDelegations
            .Include(delegation => delegation.DelegatorUser)
            .ThenInclude(user => user.UserRoles)
            .Include(delegation => delegation.DelegateUser)
            .Include(delegation => delegation.Role)
            .Where(delegation =>
                !delegation.RevokedAtUtc.HasValue &&
                delegation.StartsAtUtc <= effectiveAtUtc &&
                delegation.EndsAtUtc >= effectiveAtUtc &&
                delegation.DelegatorUser.IsActive &&
                delegation.DelegateUser.IsActive &&
                delegation.DelegatorUser.UserRoles.Any(userRole => userRole.RoleId == delegation.RoleId));
    }

    private async Task<IReadOnlyList<ApprovalDelegation>> ListActiveDelegationsInternalAsync(
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken)
    {
        if (!RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            return await BuildActiveDelegationsQuery(effectiveAtUtc)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return (await _dbContext.ApprovalDelegations
                .Include(delegation => delegation.DelegatorUser)
                .ThenInclude(user => user.UserRoles)
                .Include(delegation => delegation.DelegateUser)
                .Include(delegation => delegation.Role)
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .Where(delegation =>
                !delegation.RevokedAtUtc.HasValue &&
                delegation.StartsAtUtc <= effectiveAtUtc &&
                delegation.EndsAtUtc >= effectiveAtUtc &&
                delegation.DelegatorUser.IsActive &&
                delegation.DelegateUser.IsActive &&
                delegation.DelegatorUser.UserRoles.Any(userRole => userRole.RoleId == delegation.RoleId))
            .ToArray();
    }
}

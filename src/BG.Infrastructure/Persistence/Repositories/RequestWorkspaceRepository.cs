using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Models.Requests;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class RequestWorkspaceRepository : IRequestWorkspaceRepository
{
    private readonly BgDbContext _dbContext;

    public RequestWorkspaceRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListRequestActorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .Where(user => user.IsActive &&
                           user.UserRoles.Any(userRole =>
                               userRole.Role.RolePermissions.Any(rolePermission =>
                                   rolePermission.PermissionKey == "requests.view" ||
                                   rolePermission.PermissionKey == "requests.create")))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetRequestActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .SingleOrDefaultAsync(
                user => user.Id == userId &&
                        user.IsActive &&
                        user.UserRoles.Any(userRole =>
                            userRole.Role.RolePermissions.Any(rolePermission =>
                                rolePermission.PermissionKey == "requests.view" ||
                                rolePermission.PermissionKey == "requests.create")),
                cancellationToken);
    }

    public async Task<PagedResult<RequestListItemReadModel>> ListOwnedRequestsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.GuaranteeRequests
            .Where(request => request.RequestedByUserId == userId)
            .AsNoTracking();

        var totalItemCount = await filteredQuery.CountAsync(cancellationToken);
        var pageInfo = RepositoryPaging.CreatePageInfo(pageNumber, pageSize, totalItemCount);

        IReadOnlyList<RequestListItemPageRow> requestRows = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? RepositoryPaging.SlicePage(
                (await filteredQuery
                    .Select(request => new RequestListItemPageRow(
                        request.Id,
                        request.Guarantee.GuaranteeNumber,
                        request.Guarantee.Category,
                        request.RequestType,
                        request.Status,
                        request.RequestedAmount,
                        request.RequestedExpiryDate,
                        request.Notes,
                        request.CreatedAtUtc))
                    .ToListAsync(cancellationToken))
                    .OrderByDescending(request => request.CreatedAtUtc),
                pageInfo)
            : await filteredQuery
                .OrderByDescending(request => request.CreatedAtUtc)
                .Skip((pageInfo.PageNumber - 1) * pageInfo.PageSize)
                .Take(pageInfo.PageSize)
                .Select(request => new RequestListItemPageRow(
                    request.Id,
                    request.Guarantee.GuaranteeNumber,
                    request.Guarantee.Category,
                    request.RequestType,
                    request.Status,
                    request.RequestedAmount,
                    request.RequestedExpiryDate,
                    request.Notes,
                    request.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        IReadOnlyList<RequestListItemReadModel> requests = requestRows
            .Select(request => new RequestListItemReadModel(
                request.Id,
                request.GuaranteeNumber,
                request.GuaranteeCategory,
                request.RequestType,
                request.Status,
                request.RequestedAmount,
                request.RequestedExpiryDate,
                request.Notes,
                request.CreatedAtUtc,
                CorrespondenceCount: 0,
                CurrentStageTitleResourceKey: null,
                CurrentStageTitle: null,
                CurrentStageRoleName: null,
                LastDecisionNote: null,
                LedgerEntries: Array.Empty<RequestLedgerEntryReadModel>()))
            .ToArray();

        if (requests.Count == 0)
        {
            return new PagedResult<RequestListItemReadModel>(requests, pageInfo);
        }

        var requestIds = requests.Select(request => request.Id).ToArray();
        var correspondenceCounts = await _dbContext.GuaranteeCorrespondence
            .AsNoTracking()
            .Where(correspondence =>
                correspondence.GuaranteeRequestId.HasValue &&
                requestIds.Contains(correspondence.GuaranteeRequestId.Value))
            .GroupBy(correspondence => correspondence.GuaranteeRequestId!.Value)
            .Select(group => new
            {
                RequestId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(entry => entry.RequestId, entry => entry.Count, cancellationToken);

        var activeStageByRequestId = (await _dbContext.RequestApprovalStages
                .AsNoTracking()
                .Where(stage =>
                    stage.Status == RequestApprovalStageStatus.Active &&
                    requestIds.Contains(stage.ApprovalProcess.GuaranteeRequestId))
                .Select(stage => new ActiveApprovalStageRow(
                    stage.ApprovalProcess.GuaranteeRequestId,
                    stage.Sequence,
                    stage.TitleResourceKey,
                    stage.TitleText,
                    stage.Role != null ? stage.Role.Name : null))
                .ToListAsync(cancellationToken))
            .GroupBy(stage => stage.RequestId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(stage => stage.Sequence)
                    .First());

        var lastDecisionNoteByRequestId = await _dbContext.RequestApprovalProcesses
            .AsNoTracking()
            .Where(process => requestIds.Contains(process.GuaranteeRequestId))
            .Select(process => new
            {
                process.GuaranteeRequestId,
                Note = process.LastReturnedNote ?? process.LastRejectedNote
            })
            .ToDictionaryAsync(entry => entry.GuaranteeRequestId, entry => entry.Note, cancellationToken);

        var ledgerEntriesQuery = _dbContext.GuaranteeEvents
            .AsNoTracking()
            .Where(ledgerEntry => ledgerEntry.GuaranteeRequestId.HasValue && requestIds.Contains(ledgerEntry.GuaranteeRequestId.Value))
            .Select(ledgerEntry => new RequestLedgerEntryReadModel(
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

        List<RequestLedgerEntryReadModel> ledgerEntries;
        if (RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            ledgerEntries = (await ledgerEntriesQuery.ToListAsync(cancellationToken))
                .OrderByDescending(ledgerEntry => ledgerEntry.OccurredAtUtc)
                .ToList();
        }
        else
        {
            ledgerEntries = await ledgerEntriesQuery
                .OrderByDescending(ledgerEntry => ledgerEntry.OccurredAtUtc)
                .ToListAsync(cancellationToken);
        }

        var ledgerByRequestId = ledgerEntries
            .GroupBy(ledgerEntry => ledgerEntry.RequestId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<RequestLedgerEntryReadModel>)group.ToArray());

        var hydratedRequests = requests
            .Select(request => request with
            {
                CorrespondenceCount = correspondenceCounts.GetValueOrDefault(request.Id),
                CurrentStageTitleResourceKey = activeStageByRequestId.GetValueOrDefault(request.Id)?.TitleResourceKey,
                CurrentStageTitle = activeStageByRequestId.GetValueOrDefault(request.Id)?.TitleText,
                CurrentStageRoleName = activeStageByRequestId.GetValueOrDefault(request.Id)?.RoleName,
                LastDecisionNote = lastDecisionNoteByRequestId.GetValueOrDefault(request.Id),
                LedgerEntries = ledgerByRequestId.GetValueOrDefault(request.Id, [])
            })
            .ToArray();

        return new PagedResult<RequestListItemReadModel>(hydratedRequests, pageInfo);
    }

    public Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Guarantees
            .Include(guarantee => guarantee.Requests)
            .ThenInclude(request => request.Correspondence)
            .Include(guarantee => guarantee.Documents)
            .Include(guarantee => guarantee.Events)
            .SingleOrDefaultAsync(
                guarantee => guarantee.GuaranteeNumber == guaranteeNumber,
                cancellationToken);
    }

    public Task<GuaranteeRequest?> GetOwnedRequestByIdAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.GuaranteeRequests
            .Include(request => request.Guarantee)
            .ThenInclude(guarantee => guarantee.Events)
            .Include(request => request.RequestedByUser)
            .Include(request => request.ApprovalProcess!)
            .ThenInclude(process => process.Stages)
            .SingleOrDefaultAsync(
                request => request.Id == requestId && request.RequestedByUserId == userId,
                cancellationToken);
    }

    public void TrackCreatedRequestGraph(GuaranteeRequest request)
    {
        _dbContext.GuaranteeRequests.Add(request);
    }

    public void TrackNewApprovalProcessGraph(RequestApprovalProcess approvalProcess)
    {
        _dbContext.RequestApprovalProcesses.Add(approvalProcess);
    }

    public void TrackLedgerEvents(IEnumerable<GuaranteeEvent> ledgerEvents)
    {
        var pendingEvents = ledgerEvents.ToArray();
        if (pendingEvents.Length == 0)
        {
            return;
        }

        _dbContext.GuaranteeEvents.AddRange(pendingEvents);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record RequestListItemPageRow(
        Guid Id,
        string GuaranteeNumber,
        GuaranteeCategory GuaranteeCategory,
        GuaranteeRequestType RequestType,
        GuaranteeRequestStatus Status,
        decimal? RequestedAmount,
        DateOnly? RequestedExpiryDate,
        string? Notes,
        DateTimeOffset CreatedAtUtc);

    private sealed record ActiveApprovalStageRow(
        Guid RequestId,
        int Sequence,
        string? TitleResourceKey,
        string? TitleText,
        string? RoleName);
}

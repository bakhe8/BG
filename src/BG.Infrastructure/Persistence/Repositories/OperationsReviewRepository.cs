using BG.Application.Contracts.Persistence;
using BG.Application.Common;
using BG.Application.Operations;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class OperationsReviewRepository : IOperationsReviewRepository
{
    private readonly BgDbContext _dbContext;

    public OperationsReviewRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListOperationsActorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .Where(user => user.IsActive &&
                           user.UserRoles.Any(userRole =>
                               userRole.Role.RolePermissions.Any(rolePermission =>
                                   rolePermission.PermissionKey == "operations.queue.view" ||
                                   rolePermission.PermissionKey == "operations.queue.manage")))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetOperationsActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
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
                                rolePermission.PermissionKey == "operations.queue.manage")),
                cancellationToken);
    }

    public async Task<OperationsReviewQueuePageReadModel> ListOpenAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.OperationsReviewItems
            .Where(item => item.Status == OperationsReviewItemStatus.Pending || item.Status == OperationsReviewItemStatus.Routed)
            .AsNoTracking();

        var totalItemCount = await filteredQuery.CountAsync(cancellationToken);
        var pageInfo = RepositoryPaging.CreatePageInfo(pageNumber, pageSize, totalItemCount);

        var counts = await filteredQuery
            .GroupBy(item => 1)
            .Select(group => new OperationsReviewQueueCountsReadModel(
                group.Count(),
                group.Count(item => item.Status == OperationsReviewItemStatus.Pending),
                group.Count(item => item.Status == OperationsReviewItemStatus.Routed)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new OperationsReviewQueueCountsReadModel(0, 0, 0);

        IReadOnlyList<OperationsReviewQueueItemPageRow> itemRows = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? RepositoryPaging.SlicePage(
                (await filteredQuery
                    .Select(item => new OperationsReviewQueueItemPageRow(
                        item.Id,
                        item.GuaranteeId,
                        item.GuaranteeNumber,
                        item.ScenarioKey,
                        item.Category,
                        item.Status,
                        item.GuaranteeDocument.DocumentType,
                        item.GuaranteeDocument.FileName,
                        item.GuaranteeCorrespondence != null ? item.GuaranteeCorrespondence.ReferenceNumber : null,
                        item.CreatedAtUtc,
                        item.GuaranteeDocument.CapturedAtUtc,
                        item.GuaranteeDocument.CapturedByDisplayName,
                        item.GuaranteeDocument.CaptureChannel,
                        item.GuaranteeDocument.SourceSystemName,
                        item.GuaranteeDocument.SourceReference,
                        item.GuaranteeDocument.VerifiedDataJson,
                        item.GuaranteeCorrespondence != null ? item.GuaranteeCorrespondence.LetterDate : null,
                        item.CompletedAtUtc))
                    .ToListAsync(cancellationToken))
                    .OrderByDescending(item => item.CreatedAtUtc),
                pageInfo)
            : await filteredQuery
                .OrderByDescending(item => item.CreatedAtUtc)
                .Skip((pageInfo.PageNumber - 1) * pageInfo.PageSize)
                .Take(pageInfo.PageSize)
                .Select(item => new OperationsReviewQueueItemPageRow(
                    item.Id,
                    item.GuaranteeId,
                    item.GuaranteeNumber,
                    item.ScenarioKey,
                    item.Category,
                    item.Status,
                    item.GuaranteeDocument.DocumentType,
                    item.GuaranteeDocument.FileName,
                    item.GuaranteeCorrespondence != null ? item.GuaranteeCorrespondence.ReferenceNumber : null,
                    item.CreatedAtUtc,
                    item.GuaranteeDocument.CapturedAtUtc,
                    item.GuaranteeDocument.CapturedByDisplayName,
                    item.GuaranteeDocument.CaptureChannel,
                    item.GuaranteeDocument.SourceSystemName,
                    item.GuaranteeDocument.SourceReference,
                    item.GuaranteeDocument.VerifiedDataJson,
                    item.GuaranteeCorrespondence != null ? item.GuaranteeCorrespondence.LetterDate : null,
                    item.CompletedAtUtc))
                .ToListAsync(cancellationToken);

        var items = itemRows
            .Select(item => new OperationsReviewQueueItemReadModel(
                item.Id,
                item.GuaranteeId,
                item.GuaranteeNumber,
                item.ScenarioKey,
                item.Category,
                item.Status,
                item.DocumentType,
                item.FileName,
                item.BankReference,
                item.CreatedAtUtc,
                item.CapturedAtUtc,
                item.CapturedByDisplayName,
                item.CaptureChannel,
                item.SourceSystemName,
                item.SourceReference,
                item.VerifiedDataJson,
                item.BankLetterDate,
                item.CompletedAtUtc,
                Array.Empty<OperationsReviewRequestCandidateReadModel>()))
            .ToArray();

        if (items.Length == 0)
        {
            return new OperationsReviewQueuePageReadModel(
                new PagedResult<OperationsReviewQueueItemReadModel>(items, pageInfo),
                counts);
        }

        var guaranteeIds = items
            .Select(item => item.GuaranteeId)
            .Distinct()
            .ToArray();

        var candidateRequests = await _dbContext.GuaranteeRequests
            .AsNoTracking()
            .Where(request => guaranteeIds.Contains(request.GuaranteeId))
            .Select(request => new OperationsReviewRequestCandidateRow(
                request.GuaranteeId,
                request.Id,
                request.RequestType,
                request.Status,
                request.RequestedExpiryDate,
                request.RequestedAmount,
                request.SubmittedToBankAtUtc))
            .ToListAsync(cancellationToken);

        var requestIds = candidateRequests
            .Select(request => request.RequestId)
            .ToArray();

        var primaryDocumentByRequestId = requestIds.Length == 0
            ? new Dictionary<Guid, OperationsReviewRequestPrimaryDocumentRow>()
            : (await _dbContext.GuaranteeRequestDocuments
                    .AsNoTracking()
                    .Where(link => requestIds.Contains(link.GuaranteeRequestId))
                    .Select(link => new OperationsReviewRequestPrimaryDocumentRow(
                        link.GuaranteeRequestId,
                        link.GuaranteeDocument.DocumentType,
                        link.GuaranteeDocument.VerifiedDataJson,
                        link.LinkedAtUtc))
                    .ToListAsync(cancellationToken))
                .GroupBy(link => link.RequestId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(link => link.DocumentType == GuaranteeDocumentType.GuaranteeInstrument ? 0 : 1)
                        .ThenBy(link => link.LinkedAtUtc)
                        .First());

        var latestOutgoingReferenceByRequestId = (await _dbContext.GuaranteeCorrespondence
                .AsNoTracking()
                .Where(correspondence =>
                    correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                    correspondence.GuaranteeRequestId.HasValue &&
                    requestIds.Contains(correspondence.GuaranteeRequestId.Value))
                .Select(correspondence => new
                {
                    RequestId = correspondence.GuaranteeRequestId!.Value,
                    correspondence.ReferenceNumber,
                    correspondence.RegisteredAtUtc
                })
                .ToListAsync(cancellationToken))
            .GroupBy(correspondence => correspondence.RequestId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(correspondence => correspondence.RegisteredAtUtc)
                    .Select(correspondence => correspondence.ReferenceNumber)
                    .FirstOrDefault());

        var candidatesByGuaranteeId = candidateRequests
            .GroupBy(entry => entry.GuaranteeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OperationsReviewRequestCandidateReadModel>)group
                    .Select(entry => new OperationsReviewRequestCandidateReadModel(
                        entry.RequestId,
                        entry.RequestType,
                        entry.Status,
                        entry.RequestedExpiryDate,
                        entry.RequestedAmount,
                        entry.SubmittedToBankAtUtc,
                        latestOutgoingReferenceByRequestId.GetValueOrDefault(entry.RequestId),
                        primaryDocumentByRequestId.GetValueOrDefault(entry.RequestId)?.DocumentType,
                        primaryDocumentByRequestId.GetValueOrDefault(entry.RequestId)?.VerifiedDataJson))
                    .ToArray());

        var hydratedItems = items
            .Select(item => item with
            {
                CandidateRequests = candidatesByGuaranteeId.GetValueOrDefault(item.GuaranteeId, [])
            })
            .ToArray();

        return new OperationsReviewQueuePageReadModel(
            new PagedResult<OperationsReviewQueueItemReadModel>(hydratedItems, pageInfo),
            counts);
    }

    public async Task<IReadOnlyList<OperationsReviewRecentItemReadModel>> ListRecentlyCompletedAsync(
        int takeCount,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.OperationsReviewItems
            .Where(item => item.Status >= OperationsReviewItemStatus.Completed && item.CompletedAtUtc.HasValue)
            .AsNoTracking();

        if (RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext))
        {
            return (await filteredQuery
                    .Select(item => new OperationsReviewRecentItemReadModel(
                        item.Id,
                        item.GuaranteeNumber,
                        item.ScenarioKey,
                        item.CompletedAtUtc!.Value))
                    .ToListAsync(cancellationToken))
                .OrderByDescending(item => item.CompletedAtUtc)
                .Take(takeCount)
                .ToArray();
        }

        return await filteredQuery
            .OrderByDescending(item => item.CompletedAtUtc)
            .Take(takeCount)
            .Select(item => new OperationsReviewRecentItemReadModel(
                item.Id,
                item.GuaranteeNumber,
                item.ScenarioKey,
                item.CompletedAtUtc!.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationsReviewItem?> GetOpenItemByIdAsync(Guid reviewItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationsReviewItems
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Requests)
            .ThenInclude(request => request.RequestDocuments)
            .ThenInclude(link => link.GuaranteeDocument)
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Requests)
            .ThenInclude(request => request.Correspondence)
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Correspondence)
            .Include(item => item.GuaranteeDocument)
            .Include(item => item.GuaranteeCorrespondence)
            .SingleOrDefaultAsync(
                item => item.Id == reviewItemId && (item.Status == OperationsReviewItemStatus.Pending || item.Status == OperationsReviewItemStatus.Routed),
                cancellationToken);
    }

    public async Task<OperationsReviewItem?> GetItemByIdAsync(Guid reviewItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationsReviewItems
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Requests)
            .ThenInclude(request => request.RequestDocuments)
            .ThenInclude(link => link.GuaranteeDocument)
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Requests)
            .ThenInclude(request => request.Correspondence)
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Correspondence)
            .Include(item => item.Guarantee)
            .ThenInclude(guarantee => guarantee.Events)
            .Include(item => item.GuaranteeDocument)
            .Include(item => item.GuaranteeCorrespondence)
            .SingleOrDefaultAsync(
                item => item.Id == reviewItemId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record OperationsReviewQueueItemPageRow(
        Guid Id,
        Guid GuaranteeId,
        string GuaranteeNumber,
        string ScenarioKey,
        OperationsReviewItemCategory Category,
        OperationsReviewItemStatus Status,
        GuaranteeDocumentType DocumentType,
        string FileName,
        string? BankReference,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset CapturedAtUtc,
        string? CapturedByDisplayName,
        GuaranteeDocumentCaptureChannel CaptureChannel,
        string? SourceSystemName,
        string? SourceReference,
        string? VerifiedDataJson,
        DateOnly? BankLetterDate,
        DateTimeOffset? CompletedAtUtc);

    private sealed record OperationsReviewRequestCandidateRow(
        Guid GuaranteeId,
        Guid RequestId,
        GuaranteeRequestType RequestType,
        GuaranteeRequestStatus Status,
        DateOnly? RequestedExpiryDate,
        decimal? RequestedAmount,
        DateTimeOffset? SubmittedToBankAtUtc);

    private sealed record OperationsReviewRequestPrimaryDocumentRow(
        Guid RequestId,
        GuaranteeDocumentType DocumentType,
        string? VerifiedDataJson,
        DateTimeOffset LinkedAtUtc);
}

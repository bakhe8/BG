using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Models.Approvals;
using BG.Application.Models.Dispatch;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class DispatchWorkspaceRepository : IDispatchWorkspaceRepository
{
    private readonly BgDbContext _dbContext;

    public DispatchWorkspaceRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListDispatchActorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .Where(user => user.IsActive &&
                           user.UserRoles.Any(userRole =>
                               userRole.Role.RolePermissions.Any(rolePermission =>
                                   rolePermission.PermissionKey == "dispatch.view" ||
                                   rolePermission.PermissionKey == "dispatch.print" ||
                                   rolePermission.PermissionKey == "dispatch.record" ||
                                   rolePermission.PermissionKey == "dispatch.email")))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetDispatchActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
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
                                rolePermission.PermissionKey == "dispatch.view" ||
                                rolePermission.PermissionKey == "dispatch.print" ||
                                rolePermission.PermissionKey == "dispatch.record" ||
                                rolePermission.PermissionKey == "dispatch.email")),
                cancellationToken);
    }

    public async Task<PagedResult<DispatchQueueItemReadModel>> ListReadyRequestsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.GuaranteeRequests
            .Where(request => request.Status == GuaranteeRequestStatus.ApprovedForDispatch)
            .AsNoTracking();

        var totalItemCount = await filteredQuery.CountAsync(cancellationToken);
        var pageInfo = RepositoryPaging.CreatePageInfo(pageNumber, pageSize, totalItemCount);

        var projectedQuery = filteredQuery
            .Select(request => new DispatchQueueProjection(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                request.Guarantee.Category,
                request.RequestType,
                request.Status,
                request.RequestedByUser.DisplayName,
                request.ApprovalProcess!.CompletedAtUtc ?? request.CreatedAtUtc))
            .AsNoTracking();

        var pageItems = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? RepositoryPaging.SlicePage(
                (await projectedQuery.ToListAsync(cancellationToken))
                    .OrderBy(item => item.ReadyAtUtc),
                pageInfo)
            : await filteredQuery
                .OrderBy(request => request.ApprovalProcess!.CompletedAtUtc ?? request.CreatedAtUtc)
                .Skip((pageInfo.PageNumber - 1) * pageInfo.PageSize)
                .Take(pageInfo.PageSize)
                .Select(request => new DispatchQueueProjection(
                    request.Id,
                    request.Guarantee.GuaranteeNumber,
                    request.Guarantee.Category,
                    request.RequestType,
                    request.Status,
                    request.RequestedByUser.DisplayName,
                    request.ApprovalProcess!.CompletedAtUtc ?? request.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        var requestIds = pageItems.Select(item => item.RequestId).ToArray();
        IReadOnlyList<DispatchOutgoingLetterProjection> outgoingLetters = requestIds.Length == 0
            ? Array.Empty<DispatchOutgoingLetterProjection>()
            : await _dbContext.GuaranteeCorrespondence
                .Where(correspondence =>
                    correspondence.GuaranteeRequestId.HasValue &&
                    requestIds.Contains(correspondence.GuaranteeRequestId.Value) &&
                    correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                    correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter)
                .AsNoTracking()
                .Select(correspondence => new DispatchOutgoingLetterProjection(
                    correspondence.GuaranteeRequestId!.Value,
                    correspondence.Id,
                    correspondence.ReferenceNumber,
                    correspondence.LetterDate,
                    correspondence.PrintCount,
                    correspondence.LastPrintedAtUtc,
                    correspondence.LastPrintMode,
                    correspondence.RegisteredAtUtc))
                .ToListAsync(cancellationToken);

        var outgoingLettersByRequestId = outgoingLetters
            .GroupBy(letter => letter.RequestId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(letter => letter.RegisteredAtUtc).First());

        var sourceDocumentsByRequestId = requestIds.Length == 0
            ? new Dictionary<Guid, DispatchSourceDocumentProjection>()
            : (await _dbContext.GuaranteeRequestDocuments
                    .AsNoTracking()
                    .Where(link => requestIds.Contains(link.GuaranteeRequestId))
                    .Select(link => new DispatchSourceDocumentProjection(
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

        var items = pageItems
            .Select(item =>
            {
                outgoingLettersByRequestId.TryGetValue(item.RequestId, out var outgoingLetter);
                sourceDocumentsByRequestId.TryGetValue(item.RequestId, out var sourceDocument);

                return new DispatchQueueItemReadModel(
                    item.RequestId,
                    item.GuaranteeNumber,
                    item.GuaranteeCategory,
                    item.RequestType,
                    item.Status,
                    item.RequesterDisplayName,
                    item.ReadyAtUtc,
                    outgoingLetter?.CorrespondenceId,
                    outgoingLetter?.ReferenceNumber,
                    outgoingLetter?.LetterDate,
                    sourceDocument?.DocumentType,
                    sourceDocument?.VerifiedDataJson,
                    outgoingLetter?.PrintCount ?? 0,
                    outgoingLetter?.LastPrintedAtUtc,
                    outgoingLetter?.LastPrintMode);
            })
            .ToArray();

        return new PagedResult<DispatchQueueItemReadModel>(items, pageInfo);
    }

    public async Task<IReadOnlyList<DispatchPendingDeliveryItemReadModel>> ListPendingDeliveryAsync(
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.GuaranteeCorrespondence
            .Where(correspondence =>
                correspondence.GuaranteeRequestId.HasValue &&
                correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter &&
                correspondence.DispatchedAtUtc.HasValue &&
                !correspondence.DeliveredAtUtc.HasValue &&
                correspondence.GuaranteeRequest != null &&
                correspondence.GuaranteeRequest.Status == GuaranteeRequestStatus.AwaitingBankResponse &&
                correspondence.DispatchChannel.HasValue)
            .AsNoTracking();

        var items = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? (await filteredQuery
                .Select(correspondence => new DispatchPendingDeliveryProjection(
                    correspondence.GuaranteeRequestId!.Value,
                    correspondence.Id,
                    correspondence.Guarantee.GuaranteeNumber,
                    correspondence.Guarantee.Category,
                    correspondence.GuaranteeRequest!.RequestType,
                    correspondence.GuaranteeRequest.RequestedByUser.DisplayName,
                    correspondence.ReferenceNumber,
                    correspondence.LetterDate,
                    correspondence.DispatchChannel!.Value,
                    correspondence.DispatchReference,
                    correspondence.DispatchedAtUtc!.Value))
                .ToListAsync(cancellationToken))
                .OrderBy(item => item.DispatchedAtUtc)
                .ToArray()
            : (await filteredQuery
                .OrderBy(correspondence => correspondence.DispatchedAtUtc)
                .Select(correspondence => new DispatchPendingDeliveryProjection(
                    correspondence.GuaranteeRequestId!.Value,
                    correspondence.Id,
                    correspondence.Guarantee.GuaranteeNumber,
                    correspondence.Guarantee.Category,
                    correspondence.GuaranteeRequest!.RequestType,
                    correspondence.GuaranteeRequest.RequestedByUser.DisplayName,
                    correspondence.ReferenceNumber,
                    correspondence.LetterDate,
                    correspondence.DispatchChannel!.Value,
                    correspondence.DispatchReference,
                    correspondence.DispatchedAtUtc!.Value))
                .ToListAsync(cancellationToken))
                .ToArray();

        var requestIds = items
            .Select(item => item.RequestId)
            .Distinct()
            .ToArray();

        var sourceDocumentsByRequestId = requestIds.Length == 0
            ? new Dictionary<Guid, DispatchSourceDocumentProjection>()
            : (await _dbContext.GuaranteeRequestDocuments
                    .AsNoTracking()
                    .Where(link => requestIds.Contains(link.GuaranteeRequestId))
                    .Select(link => new DispatchSourceDocumentProjection(
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

        return items
            .Select(item =>
            {
                sourceDocumentsByRequestId.TryGetValue(item.RequestId, out var sourceDocument);

                return new DispatchPendingDeliveryItemReadModel(
                    item.RequestId,
                    item.CorrespondenceId,
                    item.GuaranteeNumber,
                    item.GuaranteeCategory,
                    item.RequestType,
                    item.RequesterDisplayName,
                    item.ReferenceNumber,
                    item.LetterDate,
                    sourceDocument?.DocumentType,
                    sourceDocument?.VerifiedDataJson,
                    item.DispatchChannel,
                    item.DispatchReference,
                    item.DispatchedAtUtc);
            })
            .ToArray();
    }

    public async Task<GuaranteeRequest?> GetRequestForDispatchAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeRequests
            .Include(request => request.Guarantee)
            .Include(request => request.RequestedByUser)
            .Include(request => request.Correspondence)
            .Include(request => request.ApprovalProcess!)
            .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalPriorSignatureReadModel>> GetApprovalSignaturesForRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RequestApprovalStages
            .Include(stage => stage.ActedByUser)
            .Include(stage => stage.ActedOnBehalfOfUser)
            .Include(stage => stage.Role)
            .Where(stage =>
                stage.ApprovalProcess.GuaranteeRequestId == requestId &&
                stage.ActedAtUtc.HasValue &&
                stage.RequiresLetterSignature)
            .OrderBy(stage => stage.Sequence)
            .Select(stage => new ApprovalPriorSignatureReadModel(
                stage.Id,
                stage.Sequence,
                stage.TitleResourceKey,
                stage.TitleText,
                stage.Role != null ? stage.Role.Name : null,
                stage.ActedAtUtc!.Value,
                stage.ActedByUserId,
                stage.ActedByUser != null ? stage.ActedByUser.DisplayName : null,
                stage.ActedOnBehalfOfUserId,
                stage.ActedOnBehalfOfUser != null ? stage.ActedOnBehalfOfUser.DisplayName : null))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public void TrackNewOutgoingCorrespondence(GuaranteeCorrespondence correspondence)
    {
        var entry = _dbContext.Entry(correspondence);
        if (entry.State is EntityState.Detached or EntityState.Unchanged or EntityState.Modified)
        {
            entry.State = EntityState.Added;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DispatchQueueProjection(
        Guid RequestId,
        string GuaranteeNumber,
        GuaranteeCategory GuaranteeCategory,
        GuaranteeRequestType RequestType,
        GuaranteeRequestStatus Status,
        string RequesterDisplayName,
        DateTimeOffset ReadyAtUtc);

    private sealed record DispatchOutgoingLetterProjection(
        Guid RequestId,
        Guid CorrespondenceId,
        string ReferenceNumber,
        DateOnly LetterDate,
        int PrintCount,
        DateTimeOffset? LastPrintedAtUtc,
        GuaranteeOutgoingLetterPrintMode? LastPrintMode,
        DateTimeOffset RegisteredAtUtc);

    private sealed record DispatchPendingDeliveryProjection(
        Guid RequestId,
        Guid CorrespondenceId,
        string GuaranteeNumber,
        GuaranteeCategory GuaranteeCategory,
        GuaranteeRequestType RequestType,
        string RequesterDisplayName,
        string ReferenceNumber,
        DateOnly LetterDate,
        GuaranteeDispatchChannel DispatchChannel,
        string? DispatchReference,
        DateTimeOffset DispatchedAtUtc);

    private sealed record DispatchSourceDocumentProjection(
        Guid RequestId,
        GuaranteeDocumentType DocumentType,
        string? VerifiedDataJson,
        DateTimeOffset LinkedAtUtc);
}

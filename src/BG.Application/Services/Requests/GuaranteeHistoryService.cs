using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Models.Requests;
using BG.Application.ReferenceData;
using BG.Application.Requests;
using BG.Domain.Guarantees;

namespace BG.Application.Services;

internal sealed class GuaranteeHistoryService : IGuaranteeHistoryService
{
    private static readonly string[] ElevatedPermissionKeys =
    [
        "approvals.queue.view",
        "approvals.sign",
        "dispatch.view",
        "operations.queue.view"
    ];

    private readonly IGuaranteeHistoryRepository _repository;

    public GuaranteeHistoryService(IGuaranteeHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<PagedResult<GuaranteeEventEntryDto>>> GetGuaranteeHistoryAsync(
        string guaranteeNumber,
        Guid requestingUserId,
        string[] requestingUserPermissions,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (requestingUserId == Guid.Empty)
        {
            return OperationResult<PagedResult<GuaranteeEventEntryDto>>.Failure(RequestErrorCodes.UserContextRequired);
        }

        if (string.IsNullOrWhiteSpace(guaranteeNumber))
        {
            return OperationResult<PagedResult<GuaranteeEventEntryDto>>.Failure(RequestErrorCodes.GuaranteeNumberRequired);
        }

        var normalizedPermissionSet = (requestingUserPermissions ?? [])
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!normalizedPermissionSet.Contains("requests.view") &&
            !ElevatedPermissionKeys.Any(normalizedPermissionSet.Contains))
        {
            return OperationResult<PagedResult<GuaranteeEventEntryDto>>.Failure(RequestErrorCodes.UserContextInvalid);
        }

        var guarantee = await _repository.GetGuaranteeWithEventsAsync(guaranteeNumber.Trim(), cancellationToken);
        if (guarantee is null)
        {
            return OperationResult<PagedResult<GuaranteeEventEntryDto>>.Failure(RequestErrorCodes.GuaranteeNotFound);
        }

        if (!CanAccessHistory(guarantee, requestingUserId, normalizedPermissionSet))
        {
            return OperationResult<PagedResult<GuaranteeEventEntryDto>>.Failure(RequestErrorCodes.UserContextInvalid);
        }

        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;
        var normalizedPageNumber = WorkspacePaging.NormalizePageNumber(pageNumber);

        var allEntries = guarantee.Events
            .OrderByDescending(ledgerEntry => ledgerEntry.OccurredAtUtc)
            .Select(MapEntry)
            .ToArray();

        var totalItemCount = allEntries.Length;
        var totalPageCount = Math.Max(1, (int)Math.Ceiling(totalItemCount / (double)normalizedPageSize));
        var effectivePageNumber = Math.Min(normalizedPageNumber, totalPageCount);

        var pagedEntries = allEntries
            .Skip((effectivePageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();

        var pageInfo = new PageInfoDto(effectivePageNumber, normalizedPageSize, totalItemCount);
        var result = new PagedResult<GuaranteeEventEntryDto>(pagedEntries, pageInfo);
        return OperationResult<PagedResult<GuaranteeEventEntryDto>>.Success(result);
    }

    private static bool CanAccessHistory(Guarantee guarantee, Guid requestingUserId, IReadOnlySet<string> permissionSet)
    {
        if (ElevatedPermissionKeys.Any(permissionSet.Contains))
        {
            return true;
        }

        return guarantee.Requests.Any(request => request.RequestedByUserId == requestingUserId);
    }

    private static GuaranteeEventEntryDto MapEntry(GuaranteeEvent ledgerEntry)
    {
        return new GuaranteeEventEntryDto(
            ledgerEntry.Id,
            GuaranteeEventResourceCatalog.GetResourceKey(ledgerEntry.EventType),
            GuaranteeEventResourceCatalog.GetIconKey(ledgerEntry.EventType),
            ledgerEntry.Summary,
            ledgerEntry.ActorDisplayName,
            ledgerEntry.OccurredAtUtc,
            ledgerEntry.PreviousAmount,
            ledgerEntry.NewAmount,
            ledgerEntry.PreviousExpiryDate,
            ledgerEntry.NewExpiryDate,
            ledgerEntry.PreviousStatus,
            ledgerEntry.NewStatus,
            ledgerEntry.ApprovalStageLabel,
            ledgerEntry.ApprovalPolicyResourceKey,
            ledgerEntry.DispatchStageResourceKey,
            ledgerEntry.OperationsScenarioTitleResourceKey,
            ledgerEntry.GuaranteeRequestId);
    }
}

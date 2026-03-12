using System.Globalization;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Dispatch;
using BG.Application.Models.Dispatch;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Domain.Identity;

namespace BG.Application.Services;

internal sealed class DispatchWorkspaceService : IDispatchWorkspaceService
{
    private readonly IDispatchWorkspaceRepository _repository;

    public DispatchWorkspaceService(IDispatchWorkspaceRepository repository)
    {
        _repository = repository;
    }

    public async Task<DispatchWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? dispatcherActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var actors = await _repository.ListDispatchActorsAsync(cancellationToken);
        var normalizedPageNumber = WorkspacePaging.NormalizePageNumber(pageNumber);

        if (actors.Count == 0)
        {
            return new DispatchWorkspaceSnapshotDto(
                null,
                [],
                [],
                [],
                new PageInfoDto(1, WorkspacePaging.DefaultPageSize, 0),
                false,
                "DispatchWorkspace_NoEligibleActor");
        }

        var activeActor = dispatcherActorId.HasValue
            ? actors.FirstOrDefault(actor => actor.Id == dispatcherActorId.Value)
            : actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        activeActor ??= actors.OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase).First();

        var items = await _repository.ListReadyRequestsAsync(
            normalizedPageNumber,
            WorkspacePaging.DefaultPageSize,
            cancellationToken);
        var pendingDeliveryItems = await _repository.ListPendingDeliveryAsync(cancellationToken);

        return new DispatchWorkspaceSnapshotDto(
            MapActor(activeActor),
            actors
                .OrderBy(actor => actor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(MapActor)
                .ToArray(),
            items.Items
                .Select(MapItem)
                .ToArray(),
            pendingDeliveryItems
                .Select(MapPendingDeliveryItem)
                .ToArray(),
            items.PageInfo,
            true,
            "DispatchWorkspace_ActorScopedNotice");
    }

    public async Task<OperationResult<PrintDispatchLetterReceiptDto>> PrintDispatchLetterAsync(
        PrintDispatchLetterCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DispatcherUserId == Guid.Empty)
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextRequired);
        }

        var actor = await _repository.GetDispatchActorByIdAsync(command.DispatcherUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextInvalid);
        }

        if (!HasPermission(actor, "dispatch.print"))
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.DispatchPrintPermissionRequired);
        }

        var referenceNumber = Normalize(command.ReferenceNumber);
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.ReferenceNumberRequired);
        }

        if (!TryParseDate(command.LetterDate, out var letterDate))
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.LetterDateRequired);
        }

        if (!command.PrintMode.HasValue)
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.PrintModeRequired);
        }

        var request = await _repository.GetRequestForDispatchAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.RequestNotFound);
        }

        try
        {
            var correspondence = request.Guarantee.RecordOutgoingLetterPrint(
                request.Id,
                referenceNumber,
                letterDate,
                command.PrintMode.Value,
                DateTimeOffset.UtcNow,
                actor.Id,
                actor.DisplayName);

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<PrintDispatchLetterReceiptDto>.Success(
                new PrintDispatchLetterReceiptDto(
                    request.Id,
                    request.Guarantee.GuaranteeNumber,
                    correspondence.ReferenceNumber,
                    correspondence.PrintCount));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("reference or date", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.OutgoingLetterReferenceMismatch);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<PrintDispatchLetterReceiptDto>.Failure(DispatchErrorCodes.RequestNotReady);
        }
    }

    public async Task<OperationResult<RecordDispatchReceiptDto>> RecordDispatchAsync(
        RecordDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DispatcherUserId == Guid.Empty)
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextRequired);
        }

        var actor = await _repository.GetDispatchActorByIdAsync(command.DispatcherUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextInvalid);
        }

        if (!command.DispatchChannel.HasValue)
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatchChannelRequired);
        }

        var dispatchChannel = command.DispatchChannel.Value;
        if (dispatchChannel == GuaranteeDispatchChannel.OfficialEmail)
        {
            if (!HasPermission(actor, "dispatch.email"))
            {
                return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatchEmailPermissionRequired);
            }
        }
        else if (!HasPermission(actor, "dispatch.record"))
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatchRecordPermissionRequired);
        }

        var referenceNumber = Normalize(command.ReferenceNumber);
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.ReferenceNumberRequired);
        }

        if (!TryParseDate(command.LetterDate, out var letterDate))
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.LetterDateRequired);
        }

        var request = await _repository.GetRequestForDispatchAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.RequestNotFound);
        }

        if (request.Status != GuaranteeRequestStatus.ApprovedForDispatch)
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.RequestNotReady);
        }

        try
        {
            var correspondence = request.Guarantee.RecordOutgoingDispatch(
                request.Id,
                referenceNumber,
                letterDate,
                dispatchChannel,
                command.DispatchReference,
                command.Note,
                DateTimeOffset.UtcNow,
                actor.Id,
                actor.DisplayName);

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<RecordDispatchReceiptDto>.Success(
                new RecordDispatchReceiptDto(
                    request.Id,
                    request.Guarantee.GuaranteeNumber,
                    correspondence.ReferenceNumber));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("reference or date", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.OutgoingLetterReferenceMismatch);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<RecordDispatchReceiptDto>.Failure(DispatchErrorCodes.RequestNotReady);
        }
    }

    public async Task<OperationResult<ConfirmDispatchDeliveryReceiptDto>> ConfirmDeliveryAsync(
        ConfirmDispatchDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DispatcherUserId == Guid.Empty)
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextRequired);
        }

        var actor = await _repository.GetDispatchActorByIdAsync(command.DispatcherUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextInvalid);
        }

        if (!HasPermission(actor, "dispatch.record") && !HasPermission(actor, "dispatch.email"))
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.DispatchRecordPermissionRequired);
        }

        var request = await _repository.GetRequestForDispatchAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.RequestNotFound);
        }

        if (request.Status != GuaranteeRequestStatus.AwaitingBankResponse)
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.DeliveryNotPending);
        }

        if (!request.Correspondence.Any(item => item.Id == command.CorrespondenceId))
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.OutgoingLetterNotFound);
        }

        try
        {
            request.Guarantee.ConfirmOutgoingDispatchDelivery(
                request.Id,
                command.CorrespondenceId,
                DateTimeOffset.UtcNow,
                command.DeliveryReference,
                command.DeliveryNote,
                actor.Id,
                actor.DisplayName);

            await _repository.SaveChangesAsync(cancellationToken);

            var correspondence = request.Correspondence.SingleOrDefault(item => item.Id == command.CorrespondenceId);
            if (correspondence is null)
            {
                return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.OutgoingLetterNotFound);
            }

            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Success(
                new ConfirmDispatchDeliveryReceiptDto(
                    request.Id,
                    request.Guarantee.GuaranteeNumber,
                    correspondence.ReferenceNumber));
        }
        catch (InvalidOperationException)
        {
            return OperationResult<ConfirmDispatchDeliveryReceiptDto>.Failure(DispatchErrorCodes.DeliveryNotPending);
        }
    }

    private static DispatchActorSummaryDto MapActor(User actor)
    {
        return new DispatchActorSummaryDto(
            actor.Id,
            actor.Username,
            actor.DisplayName,
            HasPermission(actor, "dispatch.print"),
            HasPermission(actor, "dispatch.record"),
            HasPermission(actor, "dispatch.email"));
    }

    private static DispatchQueueItemDto MapItem(DispatchQueueItemReadModel request)
    {
        return new DispatchQueueItemDto(
            request.RequestId,
            request.GuaranteeNumber,
            GuaranteeResourceCatalog.GetGuaranteeCategoryResourceKey(request.GuaranteeCategory),
            GuaranteeResourceCatalog.GetRequestTypeResourceKey(request.RequestType),
            GuaranteeResourceCatalog.GetRequestStatusResourceKey(request.Status),
            request.RequesterDisplayName,
            request.ReadyAtUtc,
            request.OutgoingCorrespondenceId,
            request.OutgoingReferenceNumber,
            request.OutgoingLetterDate,
            request.PrintCount,
            request.LastPrintedAtUtc,
            request.LastPrintMode.HasValue
                ? GuaranteeResourceCatalog.GetDispatchPrintModeResourceKey(request.LastPrintMode.Value)
                : null);
    }

    private static DispatchPendingDeliveryItemDto MapPendingDeliveryItem(DispatchPendingDeliveryItemReadModel item)
    {
        return new DispatchPendingDeliveryItemDto(
            item.RequestId,
            item.CorrespondenceId,
            item.GuaranteeNumber,
            GuaranteeResourceCatalog.GetGuaranteeCategoryResourceKey(item.GuaranteeCategory),
            GuaranteeResourceCatalog.GetRequestTypeResourceKey(item.RequestType),
            item.RequesterDisplayName,
            item.ReferenceNumber,
            item.LetterDate,
            GuaranteeResourceCatalog.GetDispatchChannelResourceKey(item.DispatchChannel),
            item.DispatchReference,
            item.DispatchedAtUtc);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            date = default;
            return false;
        }

        return DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateOnly.TryParse(normalized, out date);
    }

    private static bool HasPermission(User actor, string permissionKey)
    {
        return actor.UserRoles.Any(userRole =>
            userRole.Role.RolePermissions.Any(rolePermission =>
                string.Equals(rolePermission.PermissionKey, permissionKey, StringComparison.OrdinalIgnoreCase)));
    }
}

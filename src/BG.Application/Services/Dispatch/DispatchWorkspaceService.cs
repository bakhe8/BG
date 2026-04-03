using System.Globalization;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Dispatch;
using BG.Application.Intake;
using BG.Application.Models.Dispatch;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Domain.Identity;

namespace BG.Application.Services;

internal sealed class DispatchWorkspaceService : IDispatchWorkspaceService
{
    private readonly IDispatchWorkspaceRepository _repository;
    private readonly INotificationService _notificationService;

    public DispatchWorkspaceService(IDispatchWorkspaceRepository repository, INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
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

    public async Task<OperationResult<DispatchLetterPreviewDto>> GetLetterPreviewAsync(
        Guid dispatcherUserId,
        Guid requestId,
        string? referenceNumber,
        string? letterDate,
        CancellationToken cancellationToken = default)
    {
        if (dispatcherUserId == Guid.Empty)
        {
            return OperationResult<DispatchLetterPreviewDto>.Failure(DispatchErrorCodes.DispatcherContextRequired);
        }

        var actor = await _repository.GetDispatchActorByIdAsync(dispatcherUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<DispatchLetterPreviewDto>.Failure(DispatchErrorCodes.DispatcherContextInvalid);
        }

        var request = await _repository.GetRequestForDispatchAsync(requestId, cancellationToken);
        if (request is null)
        {
            return OperationResult<DispatchLetterPreviewDto>.Failure(DispatchErrorCodes.RequestNotFound);
        }

        if (request.Status is not GuaranteeRequestStatus.ApprovedForDispatch and not GuaranteeRequestStatus.AwaitingBankResponse and not GuaranteeRequestStatus.SubmittedToBank)
        {
            return OperationResult<DispatchLetterPreviewDto>.Failure(DispatchErrorCodes.RequestNotReady);
        }

        var latestOutgoingLetter = request.Correspondence
            .Where(correspondence =>
                correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter)
            .OrderByDescending(correspondence => correspondence.RegisteredAtUtc)
            .FirstOrDefault();

        var normalizedReferenceNumber = Normalize(referenceNumber) ?? latestOutgoingLetter?.ReferenceNumber;
        var hasLetterDate = TryParseDate(letterDate, out var parsedLetterDate);
        var effectiveLetterDate = hasLetterDate
            ? parsedLetterDate
            : latestOutgoingLetter?.LetterDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var effectiveReferenceNumber = string.IsNullOrWhiteSpace(normalizedReferenceNumber)
            ? $"DRAFT-{request.Guarantee.GuaranteeNumber}"
            : normalizedReferenceNumber;

        var matchesRecordedLetter = latestOutgoingLetter is not null &&
                                    string.Equals(latestOutgoingLetter.ReferenceNumber, effectiveReferenceNumber, StringComparison.Ordinal) &&
                                    latestOutgoingLetter.LetterDate == effectiveLetterDate;

        return OperationResult<DispatchLetterPreviewDto>.Success(
            new DispatchLetterPreviewDto(
                request.Id,
                request.Guarantee.GuaranteeNumber,
                request.Guarantee.BankName,
                request.Guarantee.BeneficiaryName,
                request.Guarantee.PrincipalName,
                request.Guarantee.CurrencyCode,
                request.Guarantee.CurrentAmount,
                request.Guarantee.IssueDate,
                request.Guarantee.ExpiryDate,
                request.RequestType,
                request.RequestedByUser.DisplayName,
                effectiveReferenceNumber,
                effectiveLetterDate,
                request.RequestedAmount,
                request.RequestedExpiryDate,
                request.Notes,
                actor.DisplayName,
                DateTimeOffset.UtcNow,
                !matchesRecordedLetter,
                latestOutgoingLetter?.PrintCount ?? 0));
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

        var existingOutgoingCorrespondenceIds = request.Correspondence
            .Where(correspondence =>
                correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter)
            .Select(correspondence => correspondence.Id)
            .ToHashSet();

        try
        {
            var printedAtUtc = DateTimeOffset.UtcNow;
            var correspondence = request.Guarantee.RecordOutgoingLetterPrint(
                request.Id,
                referenceNumber,
                letterDate,
                command.PrintMode.Value,
                printedAtUtc,
                actor.Id,
                actor.DisplayName);

            if (!existingOutgoingCorrespondenceIds.Contains(correspondence.Id))
            {
                _repository.TrackNewOutgoingCorrespondence(correspondence);
            }

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

        var existingOutgoingCorrespondenceIds = request.Correspondence
            .Where(correspondence =>
                correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter)
            .Select(correspondence => correspondence.Id)
            .ToHashSet();

        try
        {
            var dispatchedAtUtc = DateTimeOffset.UtcNow;
            var correspondence = request.Guarantee.RecordOutgoingDispatch(
                request.Id,
                referenceNumber,
                letterDate,
                dispatchChannel,
                command.DispatchReference,
                command.Note,
                dispatchedAtUtc,
                actor.Id,
                actor.DisplayName);

            if (!existingOutgoingCorrespondenceIds.Contains(correspondence.Id))
            {
                _repository.TrackNewOutgoingCorrespondence(correspondence);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            await _notificationService.SendNotificationAsync(
                NotificationMessageCatalog.LetterDispatched,
                "/Operations/Workspace",
                "operations.queue.view",
                cancellationToken: cancellationToken);

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

        if (request.Status is not GuaranteeRequestStatus.AwaitingBankResponse and not GuaranteeRequestStatus.SubmittedToBank)
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

            await _notificationService.SendNotificationAsync(
                NotificationMessageCatalog.DeliveryConfirmed,
                "/Operations/Workspace",
                "operations.queue.view",
                cancellationToken: cancellationToken);

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

    public async Task<OperationResult<ReopenDispatchReceiptDto>> ReopenDispatchAsync(
        ReopenDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DispatcherUserId == Guid.Empty)
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextRequired);
        }

        var actor = await _repository.GetDispatchActorByIdAsync(command.DispatcherUserId, cancellationToken);
        if (actor is null)
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatcherContextInvalid);
        }

        if (!HasPermission(actor, "dispatch.record") && !HasPermission(actor, "dispatch.email"))
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.DispatchRecordPermissionRequired);
        }

        var correctionNote = Normalize(command.CorrectionNote);
        if (string.IsNullOrWhiteSpace(correctionNote))
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.ReopenDispatchNoteRequired);
        }

        var request = await _repository.GetRequestForDispatchAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.RequestNotFound);
        }

        if (request.Status is not GuaranteeRequestStatus.AwaitingBankResponse and not GuaranteeRequestStatus.SubmittedToBank)
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.ReopenDispatchNotAllowed);
        }

        var correspondence = request.Correspondence.SingleOrDefault(item => item.Id == command.CorrespondenceId);
        if (correspondence is null)
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.OutgoingLetterNotFound);
        }

        try
        {
            request.Guarantee.ReopenOutgoingDispatch(
                request.Id,
                command.CorrespondenceId,
                DateTimeOffset.UtcNow,
                actor.Id,
                actor.DisplayName,
                correctionNote);

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<ReopenDispatchReceiptDto>.Success(
                new ReopenDispatchReceiptDto(
                    request.Id,
                    request.Guarantee.GuaranteeNumber,
                    correspondence.ReferenceNumber));
        }
        catch (InvalidOperationException)
        {
            return OperationResult<ReopenDispatchReceiptDto>.Failure(DispatchErrorCodes.ReopenDispatchNotAllowed);
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
            request.SourceDocumentType.HasValue
                ? GuaranteeDocumentFormCatalog.ToSnapshot(
                    IntakeVerifiedDataParser.ResolveDocumentForm(
                        request.SourceDocumentType.Value,
                        request.SourceDocumentVerifiedDataJson))
                : null,
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
            item.SourceDocumentType.HasValue
                ? GuaranteeDocumentFormCatalog.ToSnapshot(
                    IntakeVerifiedDataParser.ResolveDocumentForm(
                        item.SourceDocumentType.Value,
                        item.SourceDocumentVerifiedDataJson))
                : null,
            GuaranteeResourceCatalog.GetDispatchChannelResourceKey(item.DispatchChannel),
            item.DispatchReference,
            item.DispatchedAtUtc);
    }

    private static string? Normalize(string? value)
    {
        return StructuredInputParser.Normalize(value);
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        return StructuredInputParser.TryParseDate(value, out date);
    }

    private static bool HasPermission(User actor, string permissionKey)
    {
        return actor.UserRoles.Any(userRole =>
            userRole.Role.RolePermissions.Any(rolePermission =>
                string.Equals(rolePermission.PermissionKey, permissionKey, StringComparison.OrdinalIgnoreCase)));
    }
}

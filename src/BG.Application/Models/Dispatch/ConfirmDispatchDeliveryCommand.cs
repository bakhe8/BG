namespace BG.Application.Models.Dispatch;

public sealed record ConfirmDispatchDeliveryCommand(
    Guid DispatcherUserId,
    Guid RequestId,
    Guid CorrespondenceId,
    string? DeliveryReference,
    string? DeliveryNote);

using BG.Domain.Guarantees;

namespace BG.Application.Models.Dispatch;

public sealed record RecordDispatchCommand(
    Guid DispatcherUserId,
    Guid RequestId,
    string? ReferenceNumber,
    string? LetterDate,
    GuaranteeDispatchChannel? DispatchChannel,
    string? DispatchReference,
    string? Note);

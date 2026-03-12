using BG.Domain.Guarantees;

namespace BG.Application.Models.Dispatch;

public sealed record PrintDispatchLetterCommand(
    Guid DispatcherUserId,
    Guid RequestId,
    string? ReferenceNumber,
    string? LetterDate,
    GuaranteeOutgoingLetterPrintMode? PrintMode);

namespace BG.Application.Models.Dispatch;

public sealed record ReopenDispatchCommand(
    Guid DispatcherUserId,
    Guid RequestId,
    Guid CorrespondenceId,
    string? CorrectionNote);

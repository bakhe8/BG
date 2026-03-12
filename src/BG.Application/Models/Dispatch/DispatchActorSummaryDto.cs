namespace BG.Application.Models.Dispatch;

public sealed record DispatchActorSummaryDto(
    Guid Id,
    string Username,
    string DisplayName,
    bool CanPrint,
    bool CanRecord,
    bool CanEmail);

namespace BG.Application.Models.Intake;

public sealed record IntakeActorSummaryDto(
    Guid Id,
    string Username,
    string DisplayName);

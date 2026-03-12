namespace BG.Application.Models.Requests;

public sealed record SubmitGuaranteeRequestReceiptDto(
    Guid RequestId,
    string? CurrentStageTitleResourceKey,
    string? CurrentStageTitle,
    string? CurrentStageRoleName);

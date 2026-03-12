namespace BG.Application.Models.Approvals;

public sealed record ApprovalDecisionReceiptDto(
    Guid RequestId,
    string GuaranteeNumber,
    string OutcomeResourceKey);

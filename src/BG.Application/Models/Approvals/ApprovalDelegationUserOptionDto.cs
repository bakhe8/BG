namespace BG.Application.Models.Approvals;

public sealed record ApprovalDelegationUserOptionDto(
    Guid Id,
    string Username,
    string DisplayName);

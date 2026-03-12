namespace BG.Application.Models.Approvals;

public sealed record ApprovalDelegationRoleOptionDto(
    Guid Id,
    string Name,
    string? Description);

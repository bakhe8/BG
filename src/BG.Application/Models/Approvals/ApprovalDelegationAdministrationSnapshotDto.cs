namespace BG.Application.Models.Approvals;

public sealed record ApprovalDelegationAdministrationSnapshotDto(
    IReadOnlyList<ApprovalDelegationSummaryDto> Delegations,
    IReadOnlyList<ApprovalDelegationUserOptionDto> AvailableUsers,
    IReadOnlyList<ApprovalDelegationRoleOptionDto> AvailableRoles);

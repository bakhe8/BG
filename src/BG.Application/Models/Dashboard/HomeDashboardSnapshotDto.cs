using BG.Domain.Guarantees;

namespace BG.Application.Models.Dashboard;

public sealed record HomeDashboardSnapshotDto(
    bool IsAuthenticated,
    string? DisplayName,
    bool CanViewApprovals,
    bool CanViewRequests,
    bool CanViewOperations,
    bool CanViewDispatch,
    bool CanViewIntake,
    bool CanViewExpiringGuarantees,
    int PendingApprovalsCount,
    IReadOnlyList<HomeDashboardApprovalItemDto> PendingApprovals,
    int MyOpenRequestsCount,
    IReadOnlyList<HomeDashboardRequestItemDto> OpenRequests,
    int OperationsBacklogCount,
    int ReadyForDispatchCount,
    int PendingDeliveryCount,
    int ExpiringGuaranteesCount,
    IReadOnlyList<HomeDashboardExpiringGuaranteeDto> ExpiringGuarantees,
    IReadOnlyList<HomeDashboardIntakeActivityDto> RecentIntakeActivities)
{
    public bool HasVisibleOperationalAreas =>
        CanViewApprovals ||
        CanViewRequests ||
        CanViewOperations ||
        CanViewDispatch ||
        CanViewIntake ||
        CanViewExpiringGuarantees;

    public static HomeDashboardSnapshotDto Anonymous()
    {
        return new HomeDashboardSnapshotDto(
            false,
            null,
            false,
            false,
            false,
            false,
            false,
            false,
            0,
            [],
            0,
            [],
            0,
            0,
            0,
            0,
            [],
            []);
    }
}

public sealed record HomeDashboardApprovalItemDto(
    Guid RequestId,
    string GuaranteeNumber,
    GuaranteeRequestType RequestType,
    DateTimeOffset SubmittedAtUtc,
    string? StageTitleResourceKey,
    string? StageTitleText,
    string? RoleName);

public sealed record HomeDashboardRequestItemDto(
    Guid RequestId,
    string GuaranteeNumber,
    GuaranteeRequestType RequestType,
    GuaranteeRequestStatus Status,
    DateTimeOffset CreatedAtUtc);

public sealed record HomeDashboardExpiringGuaranteeDto(
    Guid GuaranteeId,
    string GuaranteeNumber,
    GuaranteeCategory Category,
    decimal CurrentAmount,
    string CurrencyCode,
    DateOnly ExpiryDate,
    int DaysRemaining);

public sealed record HomeDashboardIntakeActivityDto(
    Guid DocumentId,
    string GuaranteeNumber,
    string FileName,
    GuaranteeDocumentType DocumentType,
    DateTimeOffset CapturedAtUtc,
    string? CapturedByDisplayName,
    GuaranteeDocumentCaptureChannel CaptureChannel,
    string? ScenarioKey,
    string ScenarioTitleResourceKey);

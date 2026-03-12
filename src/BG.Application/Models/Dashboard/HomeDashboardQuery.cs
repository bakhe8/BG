namespace BG.Application.Models.Dashboard;

public sealed record HomeDashboardQuery(
    Guid UserId,
    bool IncludeApprovals,
    bool IncludeRequests,
    bool IncludeOperations,
    bool IncludeDispatch,
    bool IncludeIntake,
    bool IncludeExpiringGuarantees,
    DateOnly Today,
    DateTimeOffset NowUtc);

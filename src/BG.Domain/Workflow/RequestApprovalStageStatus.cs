namespace BG.Domain.Workflow;

public enum RequestApprovalStageStatus
{
    Pending = 1,
    Active = 2,
    Approved = 3,
    Returned = 4,
    Rejected = 5,
    Cancelled = 6
}

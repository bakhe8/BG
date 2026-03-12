namespace BG.Domain.Guarantees;

public enum GuaranteeRequestStatus
{
    Draft = 1,
    InApproval = 2,
    Returned = 3,
    ApprovedForDispatch = 4,
    SubmittedToBank = 5,
    AwaitingBankResponse = 6,
    Completed = 7,
    Rejected = 8,
    Cancelled = 9
}

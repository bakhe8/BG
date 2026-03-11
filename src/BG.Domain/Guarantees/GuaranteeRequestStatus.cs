namespace BG.Domain.Guarantees;

public enum GuaranteeRequestStatus
{
    Draft = 1,
    SubmittedToBank = 2,
    AwaitingBankResponse = 3,
    Completed = 4,
    Rejected = 5,
    Cancelled = 6
}

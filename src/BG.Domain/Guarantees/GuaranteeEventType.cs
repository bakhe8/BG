namespace BG.Domain.Guarantees;

public enum GuaranteeEventType
{
    Registered = 1,
    RequestRecorded = 2,
    ExpiryExtended = 3,
    AmountReduced = 4,
    Released = 5,
    Replaced = 6,
    StatusConfirmed = 7
}

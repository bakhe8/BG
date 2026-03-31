namespace BG.Domain.Guarantees;

public enum GuaranteeEventType
{
    Registered = 1,
    RequestRecorded = 2,
    ExpiryExtended = 3,
    AmountReduced = 4,
    Released = 5,
    Replaced = 6,
    StatusConfirmed = 7,
    RequestSubmittedForApproval = 8,
    ApprovalApproved = 9,
    ApprovalReturned = 10,
    ApprovalRejected = 11,
    OutgoingCorrespondenceRecorded = 12,
    IncomingCorrespondenceRecorded = 13,
    DocumentCaptured = 14,
    RequestDocumentLinked = 15,
    OutgoingLetterPrinted = 16,
    OutgoingLetterDispatched = 17,
    OutgoingLetterDelivered = 18,
    RequestUpdated = 19,
    RequestCancelled = 20,
    RequestWithdrawn = 21,
    OutgoingLetterDispatchReopened = 22,
    BankConfirmationReopened = 23
}

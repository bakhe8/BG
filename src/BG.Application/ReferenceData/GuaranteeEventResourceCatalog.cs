using BG.Domain.Guarantees;

namespace BG.Application.ReferenceData;

public static class GuaranteeEventResourceCatalog
{
    public static string GetResourceKey(GuaranteeEventType eventType)
    {
        return eventType switch
        {
            GuaranteeEventType.Registered => "GuaranteeEvent_Registered",
            GuaranteeEventType.RequestRecorded => "GuaranteeEvent_RequestRecorded",
            GuaranteeEventType.RequestUpdated => "GuaranteeEvent_RequestUpdated",
            GuaranteeEventType.RequestCancelled => "GuaranteeEvent_RequestCancelled",
            GuaranteeEventType.RequestWithdrawn => "GuaranteeEvent_RequestWithdrawn",
            GuaranteeEventType.RequestSubmittedForApproval => "GuaranteeEvent_RequestSubmittedForApproval",
            GuaranteeEventType.ApprovalApproved => "GuaranteeEvent_ApprovalApproved",
            GuaranteeEventType.ApprovalReturned => "GuaranteeEvent_ApprovalReturned",
            GuaranteeEventType.ApprovalRejected => "GuaranteeEvent_ApprovalRejected",
            GuaranteeEventType.OutgoingCorrespondenceRecorded => "GuaranteeEvent_CorrespondenceRecorded",
            GuaranteeEventType.IncomingCorrespondenceRecorded => "GuaranteeEvent_IncomingCorrespondence",
            GuaranteeEventType.DocumentCaptured => "GuaranteeEvent_DocumentCaptured",
            GuaranteeEventType.RequestDocumentLinked => "GuaranteeEvent_RequestDocumentLinked",
            GuaranteeEventType.OutgoingLetterPrinted => "GuaranteeEvent_OutgoingLetterPrinted",
            GuaranteeEventType.OutgoingLetterDispatched => "GuaranteeEvent_OutgoingLetterDispatched",
            GuaranteeEventType.OutgoingLetterDelivered => "GuaranteeEvent_OutgoingLetterDelivered",
            GuaranteeEventType.OutgoingLetterDispatchReopened => "GuaranteeEvent_DispatchReopened",
            GuaranteeEventType.ExpiryExtended => "GuaranteeEvent_ExpiryExtended",
            GuaranteeEventType.AmountReduced => "GuaranteeEvent_AmountReduced",
            GuaranteeEventType.Released => "GuaranteeEvent_Released",
            GuaranteeEventType.Replaced => "GuaranteeEvent_Replaced",
            GuaranteeEventType.StatusConfirmed => "GuaranteeEvent_StatusConfirmed",
            GuaranteeEventType.BankConfirmationReopened => "GuaranteeEvent_BankConfirmationReopened",
            _ => "GuaranteeEvent_Registered"
        };
    }

    public static string GetIconKey(GuaranteeEventType eventType)
    {
        return eventType switch
        {
            GuaranteeEventType.Registered => "bi-shield-plus",
            GuaranteeEventType.RequestRecorded => "bi-file-plus",
            GuaranteeEventType.RequestUpdated => "bi-pencil",
            GuaranteeEventType.RequestCancelled => "bi-x-circle",
            GuaranteeEventType.RequestWithdrawn => "bi-arrow-counterclockwise",
            GuaranteeEventType.RequestSubmittedForApproval => "bi-send",
            GuaranteeEventType.ApprovalApproved => "bi-check-circle-fill",
            GuaranteeEventType.ApprovalReturned => "bi-arrow-return-left",
            GuaranteeEventType.ApprovalRejected => "bi-x-circle-fill",
            GuaranteeEventType.OutgoingCorrespondenceRecorded => "bi-envelope",
            GuaranteeEventType.IncomingCorrespondenceRecorded => "bi-envelope-arrow-down",
            GuaranteeEventType.DocumentCaptured => "bi-file-earmark-arrow-up",
            GuaranteeEventType.RequestDocumentLinked => "bi-paperclip",
            GuaranteeEventType.OutgoingLetterPrinted => "bi-printer",
            GuaranteeEventType.OutgoingLetterDispatched => "bi-send-check",
            GuaranteeEventType.OutgoingLetterDelivered => "bi-envelope-check",
            GuaranteeEventType.OutgoingLetterDispatchReopened => "bi-arrow-counterclockwise",
            GuaranteeEventType.ExpiryExtended => "bi-calendar-plus",
            GuaranteeEventType.AmountReduced => "bi-currency-dollar",
            GuaranteeEventType.Released => "bi-unlock",
            GuaranteeEventType.Replaced => "bi-arrow-left-right",
            GuaranteeEventType.StatusConfirmed => "bi-patch-check",
            GuaranteeEventType.BankConfirmationReopened => "bi-arrow-counterclockwise",
            _ => "bi-shield-plus"
        };
    }
}

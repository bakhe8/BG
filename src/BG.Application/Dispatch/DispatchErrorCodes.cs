namespace BG.Application.Dispatch;

public static class DispatchErrorCodes
{
    public const string DispatcherContextRequired = "dispatch.dispatcher_context_required";
    public const string DispatcherContextInvalid = "dispatch.dispatcher_context_invalid";
    public const string DispatchPrintPermissionRequired = "dispatch.print_permission_required";
    public const string DispatchRecordPermissionRequired = "dispatch.record_permission_required";
    public const string DispatchEmailPermissionRequired = "dispatch.email_permission_required";
    public const string RequestNotFound = "dispatch.request_not_found";
    public const string RequestNotReady = "dispatch.request_not_ready";
    public const string ReferenceNumberRequired = "dispatch.reference_number_required";
    public const string LetterDateRequired = "dispatch.letter_date_required";
    public const string PrintModeRequired = "dispatch.print_mode_required";
    public const string DispatchChannelRequired = "dispatch.dispatch_channel_required";
    public const string OutgoingLetterNotFound = "dispatch.outgoing_letter_not_found";
    public const string DeliveryNotPending = "dispatch.delivery_not_pending";
    public const string OutgoingLetterReferenceMismatch = "dispatch.outgoing_letter_reference_mismatch";
    public const string ReopenDispatchNoteRequired = "dispatch.reopen_note_required";
    public const string ReopenDispatchNotAllowed = "dispatch.reopen_not_allowed";
}

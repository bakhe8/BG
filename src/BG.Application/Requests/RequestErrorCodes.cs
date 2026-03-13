namespace BG.Application.Requests;

public static class RequestErrorCodes
{
    public const string UserContextRequired = "requests.user_context_required";
    public const string UserContextInvalid = "requests.user_context_invalid";
    public const string GuaranteeNumberRequired = "requests.guarantee_number_required";
    public const string GuaranteeNotFound = "requests.guarantee_not_found";
    public const string DuplicateOpenRequest = "requests.duplicate_open_request";
    public const string RequestedAmountRequired = "requests.requested_amount_required";
    public const string RequestedExpiryDateRequired = "requests.requested_expiry_date_required";
    public const string RequestNotEditable = "requests.request_not_editable";
    public const string RequestNotCancellable = "requests.request_not_cancellable";
    public const string RequestNotWithdrawable = "requests.request_not_withdrawable";
    public const string RequestValidationFailed = "requests.request_validation_failed";
}

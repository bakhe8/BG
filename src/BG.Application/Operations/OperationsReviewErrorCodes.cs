namespace BG.Application.Operations;

public static class OperationsReviewErrorCodes
{
    public const string OperationsActorRequired = "operations.actor_required";
    public const string OperationsActorInvalid = "operations.actor_invalid";
    public const string ReviewItemNotFound = "operations.review_item_not_found";
    public const string ReviewItemNotActionable = "operations.review_item_not_actionable";
    public const string RequestRequired = "operations.request_required";
    public const string RequestNotFound = "operations.request_not_found";
    public const string RequestNotCompatible = "operations.request_not_compatible";
    public const string ResponseBankProfileMismatch = "operations.response_bank_profile_mismatch";
    public const string ConfirmedExpiryDateRequired = "operations.confirmed_expiry_date_required";
    public const string ConfirmedAmountRequired = "operations.confirmed_amount_required";
    public const string ReplacementGuaranteeNumberRequired = "operations.replacement_guarantee_number_required";
    public const string ResponseTypeNotSupported = "operations.response_type_not_supported";
}

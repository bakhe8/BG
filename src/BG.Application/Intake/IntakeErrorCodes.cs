namespace BG.Application.Intake;

public static class IntakeErrorCodes
{
    public const string IntakeActorRequired = "intake.actor_required";
    public const string IntakeActorInvalid = "intake.actor_invalid";
    public const string ScenarioRequired = "intake.scenario_required";
    public const string UnsupportedScenario = "intake.unsupported_scenario";
    public const string DocumentFormInvalid = "intake.document_form_invalid";
    public const string FileRequired = "intake.file_required";
    public const string EmptyFile = "intake.empty_file";
    public const string StagedDocumentNotFound = "intake.staged_document_not_found";
    public const string GuaranteeNumberRequired = "intake.guarantee_number_required";
    public const string DuplicateGuaranteeNumber = "intake.duplicate_guarantee_number";
    public const string GuaranteeNotFound = "intake.guarantee_not_found";
    public const string BankNameRequired = "intake.bank_name_required";
    public const string BeneficiaryRequired = "intake.beneficiary_required";
    public const string PrincipalRequired = "intake.principal_required";
    public const string GuaranteeCategoryRequired = "intake.guarantee_category_required";
    public const string AmountInvalid = "intake.amount_invalid";
    public const string CurrencyRequired = "intake.currency_required";
    public const string IssueDateRequired = "intake.issue_date_required";
    public const string ExpiryDateRequired = "intake.expiry_date_required";
    public const string ExpiryDateBeforeIssueDate = "intake.expiry_date_before_issue_date";
    public const string LetterDateRequired = "intake.letter_date_required";
    public const string BankReferenceRequired = "intake.bank_reference_required";
    public const string NewExpiryDateRequired = "intake.new_expiry_date_required";
    public const string StatusStatementRequired = "intake.status_statement_required";
    public const string AttachmentNoteRequired = "intake.attachment_note_required";
    public const string SourceSystemNameRequired = "intake.source_system_name_required";
    public const string SourceReferenceRequired = "intake.source_reference_required";
}

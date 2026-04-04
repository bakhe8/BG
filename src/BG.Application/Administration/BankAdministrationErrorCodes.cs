namespace BG.Application.Administration;

public static class BankAdministrationErrorCodes
{
    public const string BankNotFound = "bank.not_found";
    public const string CanonicalNameRequired = "bank.canonical_name_required";
    public const string ShortCodeRequired = "bank.short_code_required";
    public const string DuplicateShortCode = "bank.short_code_duplicate";
    public const string SupportedDispatchChannelsRequired = "bank.supported_dispatch_channels_required";
    public const string InvalidOfficialEmail = "bank.invalid_official_email";
    public const string OfficialEmailRequiredWhenEnabled = "bank.official_email_required_when_enabled";
}

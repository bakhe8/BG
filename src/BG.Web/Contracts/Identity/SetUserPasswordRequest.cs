namespace BG.Web.Contracts.Identity;

public sealed class SetUserPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}

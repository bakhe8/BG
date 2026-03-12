namespace BG.Application.Models.Identity;

public sealed record SetLocalUserPasswordCommand(
    Guid UserId,
    string NewPassword);

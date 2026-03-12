namespace BG.Application.Models.Identity;

public sealed record AuthenticateLocalUserCommand(
    string Username,
    string Password);

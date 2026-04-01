namespace BG.Application.Contracts.Services;

public interface ILocalPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string password);
}

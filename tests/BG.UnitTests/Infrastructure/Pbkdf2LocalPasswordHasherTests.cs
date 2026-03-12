using BG.Infrastructure.Security;

namespace BG.UnitTests.Infrastructure;

public sealed class Pbkdf2LocalPasswordHasherTests
{
    [Fact]
    public void HashPassword_and_VerifyPassword_round_trip_successfully()
    {
        var hasher = new Pbkdf2LocalPasswordHasher();

        var hash = hasher.HashPassword("correct horse battery staple");

        Assert.True(hasher.VerifyPassword(hash, "correct horse battery staple"));
        Assert.False(hasher.VerifyPassword(hash, "wrong password"));
    }
}

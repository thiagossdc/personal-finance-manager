using FluentAssertions;
using PersonalFinance.Infrastructure.Identity;

namespace PersonalFinance.Infrastructure.Tests;

public sealed class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturnDifferentHashesForSamePassword()
    {
        var password = "MySecurePassword123!";
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // Salts diferentes devem gerar hashes diferentes
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var password = "MySecurePassword123!";
        var hash = _hasher.Hash(password);

        var isValid = _hasher.Verify(password, hash);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var password = "MySecurePassword123!";
        var hash = _hasher.Hash(password);

        var isValid = _hasher.Verify("WrongPassword", hash);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Verify_InvalidHashFormat_ShouldReturnFalse()
    {
        var isValid = _hasher.Verify("password", "invalid-hash-format");

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldContainThreeParts()
    {
        var hash = _hasher.Hash("password");

        var parts = hash.Split('.');
        parts.Length.Should().Be(3);
    }
}

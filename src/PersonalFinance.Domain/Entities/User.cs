using PersonalFinance.Domain.Common;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Usuário autenticado. Armazena somente o hash de senha, nunca a senha em texto puro.
/// </summary>
public class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private User() { } // EF Core

    public static Result<User> Create(string email, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Error.Validation("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Error.Validation("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Error.Validation("Password hash is required.");
        }

        var now = DateTime.UtcNow;
        return Result<User>.Success(new User
        {
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAtUtc = DateTime.UtcNow;
        RefreshToken = null;
        RefreshTokenExpiryUtc = null;
    }

    public void SetRefreshToken(string token, TimeSpan ttl)
    {
        RefreshToken = token;
        RefreshTokenExpiryUtc = DateTime.UtcNow.Add(ttl);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsRefreshTokenValid(string token) =>
        !string.IsNullOrEmpty(RefreshToken) &&
        RefreshTokenExpiryUtc.HasValue &&
        RefreshTokenExpiryUtc.Value > DateTime.UtcNow &&
        string.Equals(RefreshToken, token, StringComparison.Ordinal);

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProfile(string displayName)
    {
        DisplayName = displayName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

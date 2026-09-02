namespace PersonalFinance.Application.Abstractions;

/// <summary>
/// Converte senhas em hashes seguros. Nunca armazene senha em texto puro.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera um hash seguro da senha informada.</summary>
    string Hash(string password);

    /// <summary>Valida se a senha confere com o hash armazenado.</summary>
    bool Verify(string password, string storedHash);
}

/// <summary>
/// Gera e valida tokens de acesso (JWT) e tokens de atualização (opacos).
/// </summary>
public interface ITokenService
{
    /// <summary>Gera um access token JWT assinado.</summary>
    TokenResult CreateAccessToken(Guid userId, string email);

    /// <summary>Gera um refresh token opaco e seguro.</summary>
    string CreateRefreshToken();

    /// <summary>Tempo de vida do refresh token.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");

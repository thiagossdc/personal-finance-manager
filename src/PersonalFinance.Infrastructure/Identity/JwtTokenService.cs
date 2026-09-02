using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PersonalFinance.Application.Abstractions;

namespace PersonalFinance.Infrastructure.Identity;

/// <summary>Gerador e validador de tokens JWT e refresh tokens opacos.</summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public TimeSpan RefreshTokenLifetime => _options.RefreshTokenLifetime;

    public TokenResult CreateAccessToken(Guid userId, string email)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(_options.AccessTokenLifetime);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("uid", userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        var secret = !string.IsNullOrEmpty(_options.SecretKey) ? _options.SecretKey : _options.Secret;
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT secret key is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Secret { get => SecretKey; set => SecretKey = value; }
    public string Issuer { get; set; } = "PersonalFinanceManager";
    public string Audience { get; set; } = "PersonalFinanceManager";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenExpirationMinutes);
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenExpirationDays);
}

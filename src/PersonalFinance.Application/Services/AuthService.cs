using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Abstractions;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Services;

public sealed class AuthService : ServiceBase, IAuthService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(IAppDbContext db, ICurrentUser currentUser, IDateTime time, IPasswordHasher passwordHasher, ITokenService tokenService)
        : base(db, currentUser, time)
    {
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return Error.Validation("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Error.Validation("Password must be at least 8 characters.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await Db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            return Error.Conflict("An account with this email already exists.");
        }

        var createResult = User.Create(email, request.DisplayName, _passwordHasher.Hash(request.Password));
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.Users.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);

        return await IssueTokensAsync(createResult.Value!, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Error.Validation("Email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Invalid email or password.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Error.Unauthorized("Invalid or expired refresh token.");
        }

        var user = await Db.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);
        if (user is null || !user.IsActive || !user.IsRefreshTokenValid(request.RefreshToken))
        {
            return Error.Unauthorized("Invalid or expired refresh token.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Error.Unauthorized("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return Error.Validation("New password must be at least 8 characters.");
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword));
        await Db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> IssueTokensAsync(User user, CancellationToken ct)
    {
        var token = _tokenService.CreateAccessToken(user.Id, user.Email);
        var refreshToken = _tokenService.CreateRefreshToken();
        user.SetRefreshToken(refreshToken, _tokenService.RefreshTokenLifetime);
        await Db.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            token.AccessToken,
            refreshToken,
            token.ExpiresAtUtc));
    }
}

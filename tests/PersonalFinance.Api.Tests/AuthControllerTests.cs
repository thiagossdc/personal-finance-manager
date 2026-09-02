using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Dtos;

namespace PersonalFinance.Api.Tests;

/// <summary>
/// Testes de autenticação. Usam a mesma factory SQLite real (FinanceWebApplicationFactory)
/// para validar o fluxo completo de registro/login/refresh e a exigência de autenticação.
/// </summary>
public sealed class AuthControllerTests : IClassFixture<FinanceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(FinanceWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidRequest_ShouldReturnSuccess()
    {
        var request = new RegisterRequest($"test_{Guid.NewGuid():N}@example.com", "Test User", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Email.Should().Be(request.Email.Trim().ToLowerInvariant());
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnConflict()
    {
        var request = new RegisterRequest($"dup_{Guid.NewGuid():N}@example.com", "Test User", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnTokens()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest(email, "Test User", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        var loginRequest = new LoginRequest($"nobody_{Guid.NewGuid():N}@example.com", "WrongPassword");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var request = new ChangePasswordRequest("CurrentPassword1!", "NewPassword1!");
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

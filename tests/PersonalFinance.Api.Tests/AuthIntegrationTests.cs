using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PersonalFinance.Application.Dtos;

namespace PersonalFinance.Api.Tests;

public class AuthIntegrationTests : IClassFixture<FinanceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(FinanceWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Register_And_Login_ReturnsTokens()
    {
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var register = new RegisterRequest(email, "Test User", "Test@12345");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", register);
        var body = await registerResponse.Content.ReadAsStringAsync();
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var login = new LoginRequest(email, "Test@12345");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", login);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await loginResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponse>>();
        envelope!.Success.Should().BeTrue();
        envelope.Data!.AccessToken.Should().NotBeNullOrEmpty();
        envelope.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var login = new LoginRequest("nonexistent@test.local", "Wrong@12345");
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    private sealed record ApiEnvelope<T>(bool Success, T? Data);
}

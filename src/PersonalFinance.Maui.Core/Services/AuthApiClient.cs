using System.Net.Http.Json;
using System.Text.Json;
using PersonalFinance.Application.Dtos;

namespace PersonalFinance.Maui.Core.Services;

public sealed class AuthApiClient : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient) => _httpClient = httpClient;

    public void SetAccessToken(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    public async Task<AuthTokenResult?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), JsonOptions, ct);
        return await ReadAuthResultAsync(response, ct);
    }

    public async Task<AuthTokenResult?> RegisterAsync(string email, string displayName, string password, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, displayName, password), JsonOptions, ct);
        return await ReadAuthResultAsync(response, ct);
    }

    private static async Task<AuthTokenResult?> ReadAuthResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponse>>(JsonOptions, ct);
        if (envelope?.Success != true || envelope.Data is null)
        {
            return null;
        }

        return new AuthTokenResult(envelope.Data.AccessToken, envelope.Data.RefreshToken, envelope.Data.ExpiresAtUtc);
    }

    private sealed record ApiEnvelope<T>(bool Success, T? Data);
}

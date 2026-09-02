using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Maui.Core.Data.Entities;

namespace PersonalFinance.Maui.Core.Sync;

/// <summary>
/// Cliente HTTP para sincronização com a API REST.
/// </summary>
public sealed class SyncApiClient : ISyncApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ISyncStateStore _stateStore;
    private string? _lastSyncToken;

    public SyncApiClient(HttpClient httpClient)
        : this(httpClient, new InMemorySyncStateStore())
    {
    }

    public SyncApiClient(HttpClient httpClient, ISyncStateStore stateStore)
    {
        _httpClient = httpClient;
        _stateStore = stateStore;
    }

    public void SetAccessToken(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<bool> PushItemAsync(SyncQueueItem item, CancellationToken ct = default)
    {
        var pushItem = new SyncPushItem(
            item.EntityName,
            item.EntityId,
            item.ClientOperationId,
            Enum.Parse<SyncChangeType>(item.ChangeType),
            item.Payload,
            item.BaseVersion);

        var request = new SyncPushRequest([pushItem]);
        var response = await _httpClient.PostAsJsonAsync("api/sync/push", request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SyncPushResult>>(JsonOptions, ct);
        return envelope?.Success == true && envelope.Data?.Applied > 0;
    }

    public async Task<int> PullChangesAsync(CancellationToken ct = default)
    {
        var request = new SyncPullRequest(_lastSyncToken ?? await _stateStore.GetLastSyncTokenAsync(ct));
        var response = await _httpClient.PostAsJsonAsync("api/sync/pull", request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SyncPullResponse>>(JsonOptions, ct);
        if (envelope?.Success != true || envelope.Data is null)
        {
            return 0;
        }

        _lastSyncToken = envelope.Data.NextSyncToken;
        await _stateStore.SetLastSyncTokenAsync(_lastSyncToken, ct);
        return envelope.Data.Changes.Count;
    }

    public async Task<IReadOnlyList<SyncServerChange>> PullChangesDetailedAsync(CancellationToken ct = default)
    {
        var request = new SyncPullRequest(_lastSyncToken ?? await _stateStore.GetLastSyncTokenAsync(ct));
        var response = await _httpClient.PostAsJsonAsync("api/sync/pull", request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SyncPullResponse>>(JsonOptions, ct);
        if (envelope?.Success != true || envelope.Data is null)
        {
            return [];
        }

        _lastSyncToken = envelope.Data.NextSyncToken;
        await _stateStore.SetLastSyncTokenAsync(_lastSyncToken, ct);
        return envelope.Data.Changes;
    }

    private sealed record ApiEnvelope<T>(bool Success, T? Data, IReadOnlyList<ApiError>? Errors);
    private sealed record ApiError(string Code, string Message);
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Sync;

namespace PersonalFinance.Maui.Core.Tests;

/// <summary>
/// Testes do cliente de sincronização: persistência do lastSyncToken entre sessões
/// (sincronização incremental) e enfileiramento/atualização do SyncEngine.
/// </summary>
public sealed class SyncApiClientTests
{
    [Fact]
    public async Task PullChanges_PersistsNextTokenInStore()
    {
        var store = new InMemorySyncStateStore();
        var handler = new FakeHttpHandler();
        handler.EnqueuePull(new SyncPullResponse([], "token-1", 0, false));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new SyncApiClient(http, store);

        var count = await client.PullChangesAsync(CancellationToken.None);

        count.Should().Be(0);
        (await store.GetLastSyncTokenAsync()).Should().Be("token-1", because: "o token deve ser persistido para sincronização incremental");
    }

    [Fact]
    public async Task PullChanges_ReusesPersistedToken_OnNextCall()
    {
        var store = new InMemorySyncStateStore();
        await store.SetLastSyncTokenAsync("token-previo");

        string? lastRequestToken = null;
        var handler = new FakeHttpHandler();
        handler.OnPull = request =>
        {
            lastRequestToken = request.LastSyncToken;
            return new SyncPullResponse([], "token-novo", 0, false);
        };

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sync = new SyncApiClient(http, store);

        await sync.PullChangesAsync(default);

        lastRequestToken.Should().Be("token-previo", because: "a chamada seguinte deve usar o token persistido");
        (await store.GetLastSyncTokenAsync()).Should().Be("token-novo");
    }

    [Fact]
    public async Task PushItem_WithDuplicateOperation_ReturnsTrueWithoutError()
    {
        var store = new InMemorySyncStateStore();
        var handler = new FakeHttpHandler();
        handler.EnqueuePush(new SyncPushResult(1, []));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sync = new SyncApiClient(http, store);

        var item = new SyncQueueItem
        {
            EntityName = "Account",
            EntityId = Guid.NewGuid().ToString(),
            ClientOperationId = "op-1",
            ChangeType = "Create",
            BaseVersion = 0,
            Payload = "{}"
        };

        var ok = await sync.PushItemAsync(item);
        ok.Should().BeTrue();
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public Func<SyncPullRequest, SyncPullResponse>? OnPull { get; set; }
        public Func<SyncPushRequest, SyncPushResult>? OnPush { get; set; }
        private readonly Queue<SyncPullResponse> _pullQueue = new();

        public void EnqueuePull(SyncPullResponse response) => _pullQueue.Enqueue(response);
        public void EnqueuePush(SyncPushResult result) => OnPush ??= _ => result;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("api/sync/pull", StringComparison.Ordinal))
            {
                var body = await request.Content!.ReadFromJsonAsync<SyncPullRequest>(cancellationToken: cancellationToken);
                var payload = OnPull?.Invoke(body!)
                    ?? (_pullQueue.Count > 0 ? _pullQueue.Dequeue() : new SyncPullResponse([], null, 0, false));
                var content = JsonSerializer.Serialize(new { success = true, data = payload, errors = Array.Empty<object>() });
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
            }

            if (request.RequestUri!.AbsolutePath.EndsWith("api/sync/push", StringComparison.Ordinal))
            {
                var body = await request.Content!.ReadFromJsonAsync<SyncPushRequest>(cancellationToken: cancellationToken);
                var result = OnPush?.Invoke(body!) ?? new SyncPushResult(0, []);
                var content = JsonSerializer.Serialize(new { success = true, data = result, errors = Array.Empty<object>() });
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}

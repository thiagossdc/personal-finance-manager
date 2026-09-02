using FluentAssertions;
using PersonalFinance.Maui.Core.Sync;

namespace PersonalFinance.Maui.Core.Tests;

/// <summary>
/// Testes do SqliteSyncStateStore: o token de sincronização incremental deve
/// sobreviver ao fechamento/reinício do app (persistência real, não em memória).
/// </summary>
public sealed class SqliteSyncStateStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"syncstate-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task SetThenGet_ReturnsPersistedToken()
    {
        var store = new SqliteSyncStateStore(_dbPath);

        await store.SetLastSyncTokenAsync("token-abc");

        (await store.GetLastSyncTokenAsync()).Should().Be("token-abc");
    }

    [Fact]
    public async Task GetWithoutSet_ReturnsNull()
    {
        var store = new SqliteSyncStateStore(_dbPath);

        (await store.GetLastSyncTokenAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Token_SurvivesStoreRecreation_SimulatingAppRestart()
    {
        var store1 = new SqliteSyncStateStore(_dbPath);
        await store1.SetLastSyncTokenAsync("token-antes-do-restart");

        // "Reinício": nova instância sobre o mesmo arquivo SQLite.
        var store2 = new SqliteSyncStateStore(_dbPath);

        (await store2.GetLastSyncTokenAsync())
            .Should().Be("token-antes-do-restart", because: "o token deve sobreviver ao reinício do app");
    }

    [Fact]
    public async Task SetNull_ClearsPersistedToken()
    {
        var store = new SqliteSyncStateStore(_dbPath);
        await store.SetLastSyncTokenAsync("token-antigo");

        await store.SetLastSyncTokenAsync(null);

        (await store.GetLastSyncTokenAsync()).Should().BeNull();

        // E após "reinício", segue nulo.
        var recreated = new SqliteSyncStateStore(_dbPath);
        (await recreated.GetLastSyncTokenAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SetOverwritesPreviousToken()
    {
        var store = new SqliteSyncStateStore(_dbPath);
        await store.SetLastSyncTokenAsync("token-1");

        await store.SetLastSyncTokenAsync("token-2");

        (await store.GetLastSyncTokenAsync()).Should().Be("token-2");
    }
}

using PersonalFinance.Maui.Core.Data.Entities;
using SQLite;

namespace PersonalFinance.Maui.Core.Sync;

/// <summary>
/// Abstração do cliente de sincronização com a API.
/// Permite testar o SyncEngine sem dependência HTTP real.
/// </summary>
public interface ISyncApiClient
{
    Task<bool> PushItemAsync(SyncQueueItem item, CancellationToken ct = default);
    Task<int> PullChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Persiste o estado de sincronização (lastSyncToken) entre sessões do app.
/// Evita re-pull de toda a base após reinício, permitindo sincronização incremental real.
/// </summary>
public interface ISyncStateStore
{
    Task<string?> GetLastSyncTokenAsync(CancellationToken ct = default);
    Task SetLastSyncTokenAsync(string? token, CancellationToken ct = default);
}

/// <summary>
/// Implementação em memória — útil como fallback e em cenários de teste/DI simples.
/// Aplicações reais devem injetar um store persistente (ex.: SQLite local/secure storage).
/// </summary>
public sealed class InMemorySyncStateStore : ISyncStateStore
{
    private string? _token;

    public Task<string?> GetLastSyncTokenAsync(CancellationToken ct = default) => Task.FromResult(_token);

    public Task SetLastSyncTokenAsync(string? token, CancellationToken ct = default)
    {
        _token = token;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Entidade persistida em SQLite que guarda o estado de sincronização entre sessões.
/// </summary>
public sealed class SyncStateRecord
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }
}

/// <summary>
/// Chave usada para persistir o token da última sincronização incremental.
/// </summary>
public static class SyncStateKeys
{
    public const string LastSyncToken = "last_sync_token";
}

/// <summary>
/// Implementação persistente de <see cref="ISyncStateStore"/> usando SQLite local.
/// Sobrevive ao fechamento/reinício do app, tornando o pull incremental real
/// (evita re-pull da base inteira). Usa conexão própria no mesmo arquivo do
/// <c>LocalDbContext</c> — SQLite suporta múltiplas conexões no mesmo path.
/// </summary>
public sealed class SqliteSyncStateStore : ISyncStateStore
{
    public const string TableName = "SyncStateRecord";

    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private readonly SQLiteAsyncConnection _db;
    private bool _initialized;

    public SqliteSyncStateStore(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        await InitLock.WaitAsync(ct);
        try
        {
            if (!_initialized)
            {
                await _db.CreateTableAsync<SyncStateRecord>();
                _initialized = true;
            }
        }
        finally
        {
            InitLock.Release();
        }
    }

    public async Task<string?> GetLastSyncTokenAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        var record = await _db.FindAsync<SyncStateRecord>(SyncStateKeys.LastSyncToken);
        return record?.Value;
    }

    public async Task SetLastSyncTokenAsync(string? token, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if (token is null)
        {
            await _db.ExecuteAsync($"DELETE FROM {TableName} WHERE Key = ?", SyncStateKeys.LastSyncToken);
            return;
        }

        await _db.InsertOrReplaceAsync(new SyncStateRecord { Key = SyncStateKeys.LastSyncToken, Value = token });
    }
}

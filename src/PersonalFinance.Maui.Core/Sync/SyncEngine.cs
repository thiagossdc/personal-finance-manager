using Microsoft.Extensions.Logging;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;

namespace PersonalFinance.Maui.Core.Sync;

public static partial class SyncEngineLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Sync failed")]
    public static partial void SyncFailed(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Pushing {Count} items to server")]
    public static partial void PushingItems(this ILogger logger, int count);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Push failed for {Entity} {Id}")]
    public static partial void PushFailed(this ILogger logger, Exception ex, string entity, string id);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Pull failed")]
    public static partial void PullFailed(this ILogger logger, Exception ex);
}

public sealed class SyncEngine : IDisposable
{
    private readonly LocalDbContext _localDb;
    private readonly ISyncApiClient _apiClient;
    private readonly SyncChangeMerger _merger;
    private readonly ILogger<SyncEngine> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int MaxRetries = 5;
    private const int BaseDelayMs = 1000;

    public SyncEngine(LocalDbContext localDb, ISyncApiClient apiClient, SyncChangeMerger merger, ILogger<SyncEngine> logger)
    {
        _localDb = localDb;
        _apiClient = apiClient;
        _merger = merger;
        _logger = logger;
    }

    /// <summary>Libera o semáforo de sincronização. A instância é singleton/scoped de longa vida no app.</summary>
    public void Dispose()
    {
        _lock.Dispose();
    }

    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var pushed = await PushAsync(ct);
            var pulled = await PullAsync(ct);
            return new SyncResult(pushed.Applied, pushed.Conflicts.Count, pulled.ChangesApplied, pulled.HasConflicts);
        }
        catch (Exception ex)
        {
            _logger.SyncFailed(ex);
            return new SyncResult(0, 0, 0, false, ex.Message);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PushResult> PushAsync(CancellationToken ct = default)
    {
        var items = await _localDb.GetPendingSyncItemsAsync();
        if (items.Count == 0)
            return new PushResult(0, []);

        _logger.PushingItems(items.Count);
        var conflicts = new List<string>();
        var applied = 0;

        foreach (var item in items)
        {
            if (item.RetryCount >= MaxRetries)
            {
                item.SyncStatus = SyncStatusNames.Failed;
                item.LastError = "Max retries exceeded";
                await _localDb.UpdateSyncItemAsync(item);
                continue;
            }

            item.SyncStatus = SyncStatusNames.Uploading;
            item.LastAttemptAt = DateTime.UtcNow;
            await _localDb.UpdateSyncItemAsync(item);

            try
            {
                var success = await _apiClient.PushItemAsync(item, ct);
                if (success)
                {
                    item.SyncStatus = SyncStatusNames.Synced;
                    await _localDb.UpdateSyncItemAsync(item);
                    applied++;
                }
                else
                {
                    item.RetryCount++;
                    item.SyncStatus = SyncStatusNames.Failed;
                    await _localDb.UpdateSyncItemAsync(item);
                }

                await Task.Delay(BaseDelayMs * item.RetryCount, ct);
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.SyncStatus = SyncStatusNames.Failed;
                item.LastError = ex.Message;
                await _localDb.UpdateSyncItemAsync(item);
                _logger.PushFailed(ex, item.EntityName, item.EntityId);
            }
        }

        return new PushResult(applied, conflicts);
    }

    public async Task<PullResult> PullAsync(CancellationToken ct = default)
    {
        try
        {
            if (_apiClient is SyncApiClient detailedClient)
            {
                var changes = await detailedClient.PullChangesDetailedAsync(ct);
                var applied = await _merger.ApplyChangesAsync(changes, ct);
                return new PullResult(applied, false);
            }

            var count = await _apiClient.PullChangesAsync(ct);
            return new PullResult(count, false);
        }
        catch (Exception ex)
        {
            _logger.PullFailed(ex);
            return new PullResult(0, false);
        }
    }
}

public sealed record SyncResult(int Pushed, int PushConflicts, int Pulled, bool PullConflicts, string? Error = null);
public sealed record PushResult(int Applied, IReadOnlyList<string> Conflicts);
public sealed record PullResult(int ChangesApplied, bool HasConflicts);

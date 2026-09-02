using SQLite;

namespace PersonalFinance.Maui.Core.Data.Entities;

/// <summary>
/// Item na fila de sincronização. Registra operações offline para envio posterior.
/// </summary>
public sealed class SyncQueueItem
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ClientOperationId { get; set; } = Guid.NewGuid().ToString();
    public string ChangeType { get; set; } = "Create";
    public string? Payload { get; set; }
    public long BaseVersion { get; set; }
    public string SyncStatus { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
}

public static class SyncStatusNames
{
    public const string Pending = "Pending";
    public const string Uploading = "Uploading";
    public const string Synced = "Synced";
    public const string Failed = "Failed";
    public const string Conflict = "Conflict";
}

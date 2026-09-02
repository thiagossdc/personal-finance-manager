using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Domain.Entities;

/// <summary>Notificação in-app (conta a vencer, orçamento atingido, fechamento de cartão, meta, sistema).</summary>
public class Notification : Entity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { } // EF Core

    public static Result<Notification> Create(Guid userId, NotificationType type, string title, string? body = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("Notification title is required.");
        }

        return Result<Notification>.Success(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Body = body,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public void MarkRead() => IsRead = true;
}

/// <summary>Trilha de auditoria de alterações sensíveis (auditoria, conformidade e depuração).</summary>
public class AuditEntry : Entity
{
    public Guid UserId { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public AuditAction Action { get; private set; }
    public string? Changes { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private AuditEntry() { } // EF Core

    public static AuditEntry Create(Guid userId, string entityName, string entityId, AuditAction action, string? changes = null) =>
        new()
        {
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Changes = changes,
            OccurredAtUtc = DateTime.UtcNow
        };
}

/// <summary>
/// Entrada da fila de sincronização offline-first. Representa uma operação local pendente
/// de upload para o servidor, com estado, tentativas e idempotência por ClientOperationId.
/// </summary>
public class SyncItem : Entity
{
    public Guid UserId { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public SyncChangeType ChangeType { get; private set; }
    public SyncStatus Status { get; private set; }
    public string ClientOperationId { get; private set; } = string.Empty;
    public string? Payload { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? NextRetryAtUtc { get; private set; }
    public long? ServerVersion { get; private set; }
    public DateTime? SyncedAtUtc { get; private set; }

    private SyncItem() { } // EF Core

    public static Result<SyncItem> Create(
        Guid userId,
        string entityName,
        string entityId,
        SyncChangeType changeType,
        string clientOperationId,
        string? payload = null)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return Error.Validation("Entity name is required.");
        }

        if (string.IsNullOrWhiteSpace(clientOperationId))
        {
            return Error.Validation("Client operation id is required.");
        }

        var now = DateTime.UtcNow;
        return Result<SyncItem>.Success(new SyncItem
        {
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            ChangeType = changeType,
            ClientOperationId = clientOperationId,
            Payload = payload,
            Status = SyncStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    public void StartProcessing()
    {
        Status = SyncStatus.InProgress;
        AttemptCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSynced(long serverVersion)
    {
        Status = SyncStatus.Synced;
        ServerVersion = serverVersion;
        SyncedAtUtc = DateTime.UtcNow;
        LastError = null;
        NextRetryAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca como falha e programa nova tentativa com backoff exponencial baseado no número de tentativas.
    /// </summary>
    public void MarkFailed(string error, int maxAttempts = 5)
    {
        LastError = error;
        UpdatedAtUtc = DateTime.UtcNow;

        if (AttemptCount >= maxAttempts)
        {
            Status = SyncStatus.Failed;
            NextRetryAtUtc = null;
            return;
        }

        // Backoff exponencial: 2^n segundos (até um teto de ~8 min).
        var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, AttemptCount)));
        NextRetryAtUtc = DateTime.UtcNow.Add(delay);
        Status = SyncStatus.Pending;
    }

    public void ResetRetryState()
    {
        Status = SyncStatus.Pending;
        NextRetryAtUtc = null;
        LastError = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

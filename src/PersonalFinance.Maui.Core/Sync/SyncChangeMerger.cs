using System.Text.Json;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;

namespace PersonalFinance.Maui.Core.Sync;

/// <summary>
/// Aplica alterações recebidas do servidor no banco local SQLite.
/// Estratégia: last-write-wins por versão — registros com versão maior substituem locais.
/// </summary>
public sealed class SyncChangeMerger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LocalDbContext _localDb;

    public SyncChangeMerger(LocalDbContext localDb) => _localDb = localDb;

    public async Task<int> ApplyChangesAsync(IReadOnlyList<SyncServerChange> changes, CancellationToken ct = default)
    {
        var applied = 0;
        foreach (var change in changes)
        {
            var success = change.EntityName switch
            {
                "Account" => await ApplyAccountChangeAsync(change, ct),
                "Category" => await ApplyCategoryChangeAsync(change, ct),
                "Transaction" => await ApplyTransactionChangeAsync(change, ct),
                "CreditCard" => await ApplyCreditCardChangeAsync(change, ct),
                "Budget" => await ApplyBudgetChangeAsync(change, ct),
                "Goal" => await ApplyGoalChangeAsync(change, ct),
                "RecurringTransaction" => await ApplyRecurringChangeAsync(change, ct),
                "Transfer" => await ApplyTransferChangeAsync(change, ct),
                _ => false
            };

            if (success)
            {
                applied++;
            }
        }

        return applied;
    }

    private async Task<bool> ApplyAccountChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (change.ChangeType == Domain.Enums.SyncChangeType.Delete)
        {
            var existing = await _localDb.GetAccountAsync(change.EntityId);
            if (existing is not null)
            {
                existing.IsActive = false;
                await _localDb.SaveAccountAsync(existing);
            }

            return true;
        }

        if (string.IsNullOrEmpty(change.Payload))
        {
            return false;
        }

        var dto = JsonSerializer.Deserialize<AccountSyncPayload>(change.Payload, JsonOptions);
        if (dto is null)
        {
            return false;
        }

        var local = await _localDb.GetAccountAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version)
        {
            return false;
        }

        local ??= new LocalAccount { Id = change.EntityId };
        local.Name = dto.Name;
        local.Type = dto.Type;
        local.InitialBalance = dto.InitialBalance;
        local.CurrentBalance = dto.CurrentBalance;
        local.Currency = dto.Currency;
        local.IsActive = dto.IsActive;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveAccountAsync(local);
        return true;
    }

    private async Task<bool> ApplyCategoryChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (change.ChangeType == Domain.Enums.SyncChangeType.Delete)
        {
            var existing = await _localDb.GetCategoryAsync(change.EntityId);
            if (existing is not null)
            {
                existing.IsActive = false;
                await _localDb.SaveCategoryAsync(existing);
            }
            return true;
        }

        if (string.IsNullOrEmpty(change.Payload))
        {
            return false;
        }

        var dto = JsonSerializer.Deserialize<CategorySyncPayload>(change.Payload, JsonOptions);
        if (dto is null)
        {
            return false;
        }

        var local = await _localDb.GetCategoryAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version)
        {
            return false;
        }

        local ??= new LocalCategory { Id = change.EntityId };
        local.Name = dto.Name;
        local.Icon = dto.Icon;
        local.Type = dto.Type;
        local.ParentId = dto.ParentId;
        local.IsActive = dto.IsActive;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveCategoryAsync(local);
        return true;
    }

    private async Task<bool> ApplyTransactionChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (change.ChangeType == Domain.Enums.SyncChangeType.Delete)
        {
            var existing = await _localDb.GetTransactionAsync(change.EntityId);
            if (existing is not null)
            {
                existing.Status = "Canceled";
                await _localDb.SaveTransactionAsync(existing);
            }
            return true;
        }

        if (string.IsNullOrEmpty(change.Payload))
        {
            return false;
        }

        var dto = JsonSerializer.Deserialize<TransactionSyncPayload>(change.Payload, JsonOptions);
        if (dto is null)
        {
            return false;
        }

        var local = await _localDb.GetTransactionAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version)
        {
            return false;
        }

        local ??= new LocalTransaction { Id = change.EntityId };
        local.AccountId = dto.AccountId;
        local.CategoryId = dto.CategoryId;
        local.Type = dto.Type;
        local.Amount = dto.Amount;
        local.Description = dto.Description;
        local.TransactionDate = dto.TransactionDate;
        local.Status = dto.Status;
        local.Note = dto.Note;
        local.TransferId = dto.TransferId;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveTransactionAsync(local);
        return true;
    }

    private async Task<bool> ApplyCreditCardChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(change.Payload)) return false;
        var dto = JsonSerializer.Deserialize<CreditCardSyncPayload>(change.Payload, JsonOptions);
        if (dto is null) return false;

        var local = await _localDb.GetCreditCardAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version) return false;

        local ??= new LocalCreditCard { Id = change.EntityId };
        local.Name = dto.Name;
        local.CreditLimit = dto.CreditLimit;
        local.ClosingDay = dto.ClosingDay;
        local.DueDay = dto.DueDay;
        local.LastFourDigits = dto.LastFourDigits;
        local.OpenAmount = dto.OpenAmount;
        local.Status = dto.Status;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveCreditCardAsync(local);
        return true;
    }

    private async Task<bool> ApplyBudgetChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(change.Payload)) return false;
        var dto = JsonSerializer.Deserialize<BudgetSyncPayload>(change.Payload, JsonOptions);
        if (dto is null) return false;

        var local = await _localDb.GetBudgetAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version) return false;

        local ??= new LocalBudget { Id = change.EntityId };
        local.CategoryId = dto.CategoryId;
        local.Limit = dto.Limit;
        local.Period = dto.Period;
        local.Month = dto.Month;
        local.Year = dto.Year;
        local.AlertThreshold = dto.AlertThreshold;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveBudgetAsync(local);
        return true;
    }

    private async Task<bool> ApplyGoalChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(change.Payload)) return false;
        var dto = JsonSerializer.Deserialize<GoalSyncPayload>(change.Payload, JsonOptions);
        if (dto is null) return false;

        var local = await _localDb.GetGoalAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version) return false;

        local ??= new LocalGoal { Id = change.EntityId };
        local.Name = dto.Name;
        local.TargetAmount = dto.TargetAmount;
        local.CurrentAmount = dto.CurrentAmount;
        local.Deadline = dto.Deadline;
        local.Status = dto.Status;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveGoalAsync(local);
        return true;
    }

    private async Task<bool> ApplyRecurringChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(change.Payload)) return false;
        var dto = JsonSerializer.Deserialize<RecurringSyncPayload>(change.Payload, JsonOptions);
        if (dto is null) return false;

        var local = await _localDb.GetRecurringTransactionAsync(change.EntityId);
        if (local is not null && local.Version >= change.Version) return false;

        local ??= new LocalRecurringTransaction { Id = change.EntityId };
        local.AccountId = dto.AccountId;
        local.CategoryId = dto.CategoryId;
        local.Type = dto.Type;
        local.Amount = dto.Amount;
        local.Description = dto.Description;
        local.Frequency = dto.Frequency;
        local.StartDate = dto.StartDate;
        local.NextExecution = dto.NextExecution;
        local.IsActive = dto.IsActive;
        local.UpdatedAt = dto.UpdatedAtUtc;
        local.Version = change.Version;
        await _localDb.SaveRecurringTransactionAsync(local);
        return true;
    }

    private async Task<bool> ApplyTransferChangeAsync(SyncServerChange change, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(change.Payload)) return false;
        var dto = JsonSerializer.Deserialize<TransferSyncPayload>(change.Payload, JsonOptions);
        if (dto is null) return false;

        var local = new LocalTransfer
        {
            Id = change.EntityId,
            SourceAccountId = dto.SourceAccountId,
            TargetAccountId = dto.TargetAccountId,
            Amount = dto.Amount,
            Description = dto.Description,
            TransferDate = dto.TransferDate,
            UpdatedAt = dto.UpdatedAtUtc,
            Version = change.Version
        };
        await _localDb.SaveTransferAsync(local);
        return true;
    }

    private sealed record AccountSyncPayload(
        string Name, string Type, decimal InitialBalance, decimal CurrentBalance,
        string Currency, bool IsActive, DateTime UpdatedAtUtc);

    private sealed record CategorySyncPayload(
        string Name, string? Icon, string Type, string? ParentId, bool IsActive, DateTime UpdatedAtUtc);

    private sealed record TransactionSyncPayload(
        string AccountId, string? CategoryId, string Type, decimal Amount,
        string Description, DateTime TransactionDate, string Status, string? Note,
        string? TransferId, DateTime UpdatedAtUtc);

    private sealed record CreditCardSyncPayload(
        string Name, decimal CreditLimit, int ClosingDay, int DueDay, string? LastFourDigits,
        decimal OpenAmount, string Status, DateTime UpdatedAtUtc);

    private sealed record BudgetSyncPayload(
        string CategoryId, decimal Limit, string Period, int? Month, int? Year,
        decimal AlertThreshold, DateTime UpdatedAtUtc);

    private sealed record GoalSyncPayload(
        string Name, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline,
        string Status, DateTime UpdatedAtUtc);

    private sealed record RecurringSyncPayload(
        string AccountId, string? CategoryId, string Type, decimal Amount, string Description,
        string Frequency, DateTime StartDate, DateTime? NextExecution, bool IsActive, DateTime UpdatedAtUtc);

    private sealed record TransferSyncPayload(
        string SourceAccountId, string TargetAccountId, decimal Amount, string? Description,
        DateTime TransferDate, DateTime UpdatedAtUtc);
}

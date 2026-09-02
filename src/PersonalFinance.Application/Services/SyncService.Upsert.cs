using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed partial class SyncService
{
    private async Task<Result<long>> UpsertAccountAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncAccountPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null)
        {
            return Result<long>.Failure(Error.Validation("Invalid account payload."));
        }

        var existing = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == entityId && a.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version)
            {
                return Result<long>.Failure(Error.Conflict("Account update conflicts with a newer server version."));
            }

            var rename = existing.Rename(payload.Name);
            return rename.IsSuccess ? Result<long>.Success(existing.Version) : Result<long>.Failure(rename.Error!);
        }

        if (item.ChangeType == SyncChangeType.Update)
        {
            return Result<long>.Failure(Error.NotFound("Account not found for update."));
        }

        var create = Account.Create(userId, payload.Name, payload.Type, Money.From(payload.InitialBalance, payload.Currency ?? "BRL"));
        if (create.IsFailure)
        {
            return Result<long>.Failure(create.Error!);
        }

        var account = create.Value!;
        account.AssignClientId(entityId);
        Db.Accounts.Add(account);
        return Result<long>.Success(account.Version);
    }

    private async Task<Result<long>> UpsertTransactionAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncTransactionPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null)
        {
            return Result<long>.Failure(Error.Validation("Invalid transaction payload."));
        }

        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == payload.AccountId && a.UserId == userId, ct);
        if (account is null)
        {
            return Result<long>.Failure(Error.NotFound("Account for transaction was not found."));
        }

        var existing = await Db.Transactions.FirstOrDefaultAsync(t => t.Id == entityId && t.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version)
            {
                return Result<long>.Failure(Error.Conflict("Transaction update conflicts with a newer server version."));
            }

            var update = existing.Update(payload.TransactionDate, Money.From(payload.Amount, account.Currency), payload.Description, payload.CategoryId, payload.Note);
            return update.IsSuccess ? Result<long>.Success(existing.Version) : Result<long>.Failure(update.Error!);
        }

        if (item.ChangeType == SyncChangeType.Update)
        {
            return Result<long>.Failure(Error.NotFound("Transaction not found for update."));
        }

        var create = payload.Type == TransactionType.Income
            ? Transaction.CreateIncome(userId, account.Id, Money.From(payload.Amount, account.Currency), payload.Description, payload.TransactionDate, payload.CategoryId, payload.Note)
            : Transaction.CreateExpense(userId, account.Id, Money.From(payload.Amount, account.Currency), payload.Description, payload.TransactionDate, payload.CategoryId, payload.Note);
        if (create.IsFailure)
        {
            return Result<long>.Failure(create.Error!);
        }

        var txn = create.Value!;
        txn.AssignClientId(entityId);
        Db.Transactions.Add(txn);
        return Result<long>.Success(txn.Version);
    }

    private async Task<Result<long>> UpsertCategoryAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncCategoryPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null)
        {
            return Result<long>.Failure(Error.Validation("Invalid category payload."));
        }

        var existing = await Db.Categories.FirstOrDefaultAsync(c => c.Id == entityId && c.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version)
            {
                return Result<long>.Failure(Error.Conflict("Category update conflicts with a newer server version."));
            }

            existing.Update(payload.Name, payload.Icon);
            return Result<long>.Success(existing.Version);
        }

        if (item.ChangeType == SyncChangeType.Update)
        {
            return Result<long>.Failure(Error.NotFound("Category not found for update."));
        }

        var create = Category.Create(userId, payload.Name, payload.Type, payload.Icon, payload.ParentId);
        if (create.IsFailure)
        {
            return Result<long>.Failure(create.Error!);
        }

        var category = create.Value!;
        category.AssignClientId(entityId);
        Db.Categories.Add(category);
        return Result<long>.Success(category.Version);
    }

    private async Task<Result<long>> UpsertCreditCardAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncCreditCardPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null) return Result<long>.Failure(Error.Validation("Invalid credit card payload."));

        var existing = await Db.CreditCards.FirstOrDefaultAsync(c => c.Id == entityId && c.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version) return Result<long>.Failure(Error.Conflict("Credit card conflict."));
            return Result<long>.Success(existing.Version);
        }

        if (item.ChangeType == SyncChangeType.Update) return Result<long>.Failure(Error.NotFound("Card not found."));

        var create = CreditCard.Create(userId, payload.Name, Money.From(payload.CreditLimit), payload.ClosingDay, payload.DueDay, payload.LastFourDigits);
        if (create.IsFailure) return Result<long>.Failure(create.Error!);

        var card = create.Value!;
        card.AssignClientId(entityId);
        Db.CreditCards.Add(card);
        return Result<long>.Success(card.Version);
    }

    private async Task<Result<long>> UpsertBudgetAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncBudgetPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null) return Result<long>.Failure(Error.Validation("Invalid budget payload."));

        var existing = await Db.Budgets.FirstOrDefaultAsync(b => b.Id == entityId && b.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version) return Result<long>.Failure(Error.Conflict("Budget conflict."));
            var upd = existing.UpdateLimit(Money.From(payload.Limit));
            return upd.IsSuccess ? Result<long>.Success(existing.Version) : Result<long>.Failure(upd.Error!);
        }

        if (item.ChangeType == SyncChangeType.Update) return Result<long>.Failure(Error.NotFound("Budget not found."));

        var create = Budget.Create(userId, payload.CategoryId, Money.From(payload.Limit), payload.Period, payload.Month, payload.Year, payload.AlertThreshold);
        if (create.IsFailure) return Result<long>.Failure(create.Error!);

        var budget = create.Value!;
        budget.AssignClientId(entityId);
        Db.Budgets.Add(budget);
        return Result<long>.Success(budget.Version);
    }

    private async Task<Result<long>> UpsertGoalAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncGoalPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null) return Result<long>.Failure(Error.Validation("Invalid goal payload."));

        var existing = await Db.FinancialGoals.FirstOrDefaultAsync(g => g.Id == entityId && g.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version) return Result<long>.Failure(Error.Conflict("Goal conflict."));
            return Result<long>.Success(existing.Version);
        }

        if (item.ChangeType == SyncChangeType.Update) return Result<long>.Failure(Error.NotFound("Goal not found."));

        var create = FinancialGoal.Create(userId, payload.Name, Money.From(payload.TargetAmount), payload.Deadline);
        if (create.IsFailure) return Result<long>.Failure(create.Error!);

        var goal = create.Value!;
        goal.AssignClientId(entityId);
        Db.FinancialGoals.Add(goal);
        return Result<long>.Success(goal.Version);
    }

    private async Task<Result<long>> UpsertRecurringTransactionAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.Parse(item.EntityId);
        var payload = JsonSerializer.Deserialize<SyncRecurringPayload>(item.Payload ?? "{}", JsonOptions);
        if (payload is null) return Result<long>.Failure(Error.Validation("Invalid recurring payload."));

        var existing = await Db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == entityId && r.UserId == userId, ct);
        if (existing is not null)
        {
            if (item.BaseVersion < existing.Version) return Result<long>.Failure(Error.Conflict("Recurring conflict."));
            return Result<long>.Success(existing.Version);
        }

        if (item.ChangeType == SyncChangeType.Update) return Result<long>.Failure(Error.NotFound("Recurring not found."));

        var create = RecurringTransaction.Create(userId, payload.AccountId, payload.Type, Money.From(payload.Amount), payload.Description, payload.Frequency, payload.StartDate, payload.CategoryId);
        if (create.IsFailure) return Result<long>.Failure(create.Error!);

        var rec = create.Value!;
        rec.AssignClientId(entityId);
        Db.RecurringTransactions.Add(rec);
        return Result<long>.Success(rec.Version);
    }

    private sealed record SyncCreditCardPayload(string Name, decimal CreditLimit, int ClosingDay, int DueDay, string? LastFourDigits);
    private sealed record SyncBudgetPayload(Guid CategoryId, decimal Limit, BudgetPeriod Period, int? Month, int? Year, decimal AlertThreshold);
    private sealed record SyncGoalPayload(string Name, decimal TargetAmount, DateTime? Deadline);
    private sealed record SyncRecurringPayload(Guid AccountId, TransactionType Type, decimal Amount, string Description, RecurrenceFrequency Frequency, DateTime StartDate, Guid? CategoryId);
}

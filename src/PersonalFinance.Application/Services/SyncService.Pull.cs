using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Services;

public sealed partial class SyncService
{
    public async Task<Result<SyncPullResponse>> PullAsync(SyncPullRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        DateTime since = DateTime.MinValue.ToUniversalTime();
        if (DateTime.TryParse(request.LastSyncToken, out var parsed))
        {
            since = parsed.ToUniversalTime();
        }

        var accounts = await Db.Accounts.Where(a => a.UserId == userId && a.UpdatedAtUtc > since).ToListAsync(ct);
        var transactions = await Db.Transactions.Where(t => t.UserId == userId && t.UpdatedAtUtc > since).ToListAsync(ct);
        var categories = await Db.Categories.Where(c => c.UserId == userId && c.UpdatedAtUtc > since).ToListAsync(ct);
        var cards = await Db.CreditCards.Where(c => c.UserId == userId && c.UpdatedAtUtc > since).ToListAsync(ct);
        var budgets = await Db.Budgets.Where(b => b.UserId == userId && b.UpdatedAtUtc > since).ToListAsync(ct);
        var goals = await Db.FinancialGoals.Where(g => g.UserId == userId && g.UpdatedAtUtc > since).ToListAsync(ct);
        var recurrings = await Db.RecurringTransactions.Where(r => r.UserId == userId && r.UpdatedAtUtc > since).ToListAsync(ct);

        var changes = new List<SyncServerChange>();
        var latest = since;

        foreach (var a in accounts)
        {
            changes.Add(new SyncServerChange("Account", a.Id.ToString(), a.IsActive ? SyncChangeType.Update : SyncChangeType.Delete, a.Version,
                JsonSerializer.Serialize(new SyncAccountPayload(a.Name, a.Type, a.InitialBalance.Amount, a.Currency, a.IsActive), JsonOptions)));
            if (a.UpdatedAtUtc > latest) latest = a.UpdatedAtUtc;
        }

        foreach (var t in transactions)
        {
            changes.Add(new SyncServerChange("Transaction", t.Id.ToString(), t.Status == TransactionStatus.Canceled ? SyncChangeType.Delete : SyncChangeType.Update, t.Version,
                JsonSerializer.Serialize(new SyncTransactionPayload(t.AccountId, t.Type, t.Amount.Amount, t.Description, t.TransactionDate, t.CategoryId, t.Note, t.Status), JsonOptions)));
            if (t.UpdatedAtUtc > latest) latest = t.UpdatedAtUtc;
        }

        foreach (var c in categories)
        {
            changes.Add(new SyncServerChange("Category", c.Id.ToString(), c.IsActive ? SyncChangeType.Update : SyncChangeType.Delete, c.Version,
                JsonSerializer.Serialize(new SyncCategoryPayload(c.Name, c.Type, c.Icon, c.ParentId), JsonOptions)));
            if (c.UpdatedAtUtc > latest) latest = c.UpdatedAtUtc;
        }

        foreach (var cc in cards)
        {
            changes.Add(new SyncServerChange("CreditCard", cc.Id.ToString(), cc.Status == CreditCardStatus.Active ? SyncChangeType.Update : SyncChangeType.Delete, cc.Version,
                JsonSerializer.Serialize(new SyncCreditCardPayload(cc.Name, cc.CreditLimit.Amount, cc.ClosingDay, cc.DueDay, cc.LastFourDigits), JsonOptions)));
            if (cc.UpdatedAtUtc > latest) latest = cc.UpdatedAtUtc;
        }

        foreach (var b in budgets)
        {
            changes.Add(new SyncServerChange("Budget", b.Id.ToString(), SyncChangeType.Update, b.Version,
                JsonSerializer.Serialize(new SyncBudgetPayload(b.CategoryId, b.Limit.Amount, b.Period, b.Month, b.Year, b.AlertThreshold), JsonOptions)));
            if (b.UpdatedAtUtc > latest) latest = b.UpdatedAtUtc;
        }

        foreach (var g in goals)
        {
            changes.Add(new SyncServerChange("Goal", g.Id.ToString(), g.Status != GoalStatus.Cancelled ? SyncChangeType.Update : SyncChangeType.Delete, g.Version,
                JsonSerializer.Serialize(new SyncGoalPayload(g.Name, g.TargetAmount.Amount, g.Deadline), JsonOptions)));
            if (g.UpdatedAtUtc > latest) latest = g.UpdatedAtUtc;
        }

        foreach (var r in recurrings)
        {
            changes.Add(new SyncServerChange("RecurringTransaction", r.Id.ToString(), r.IsActive ? SyncChangeType.Update : SyncChangeType.Delete, r.Version,
                JsonSerializer.Serialize(new SyncRecurringPayload(r.AccountId, r.Type, r.Amount.Amount, r.Description, r.Frequency, r.StartDate, r.CategoryId), JsonOptions)));
            if (r.UpdatedAtUtc > latest) latest = r.UpdatedAtUtc;
        }

        var total = changes.Count;
        var pageSize = Math.Clamp(request.PageSize, 1, 500);
        var paged = changes.OrderBy(x => x.Version).Skip((request.Page - 1) * pageSize).Take(pageSize).ToList();
        var hasMore = request.Page * pageSize < total;

        var nextToken = hasMore ? request.LastSyncToken : latest.ToString("O");

        return Result<SyncPullResponse>.Success(new SyncPullResponse(paged, nextToken, total, hasMore));
    }

    // Payloads de sincronização (contrato explícito entre dispositivo e servidor).
    private sealed record SyncAccountPayload(string Name, AccountType Type, decimal InitialBalance, string? Currency, bool IsActive);
    private sealed record SyncTransactionPayload(Guid AccountId, TransactionType Type, decimal Amount, string Description, DateTime TransactionDate, Guid? CategoryId, string? Note, TransactionStatus Status);
    private sealed record SyncCategoryPayload(string Name, TransactionType Type, string? Icon, Guid? ParentId);
}

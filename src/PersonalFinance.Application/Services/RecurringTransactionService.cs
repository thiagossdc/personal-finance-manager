using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed class RecurringTransactionService : ServiceBase, IRecurringTransactionService
{
    private readonly BalanceCalculator _balance;

    public RecurringTransactionService(IAppDbContext db, ICurrentUser currentUser, IDateTime time, BalanceCalculator balance)
        : base(db, currentUser, time)
    {
        _balance = balance;
    }

    public async Task<Result<IReadOnlyList<RecurringTransactionDto>>> GetAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var items = await Db.RecurringTransactions
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.NextExecution)
            .ToListAsync(ct);
        return Result<IReadOnlyList<RecurringTransactionDto>>.Success(items.Select(r => r.ToDto()).ToList());
    }

    public async Task<Result<RecurringTransactionDto>> CreateAsync(CreateRecurringTransactionRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, ct);
        if (account is null)
        {
            return Error.NotFound("Account was not found.");
        }

        var createResult = RecurringTransaction.Create(
            userId, request.AccountId, request.Type, Money.From(request.Amount, account.Currency),
            request.Description, request.Frequency, request.StartDate, request.CategoryId, request.Interval, request.EndDate);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.RecurringTransactions.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);
        return Result<RecurringTransactionDto>.Success(createResult.Value!.ToDto());
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var item = await Db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
        if (item is null)
        {
            return Error.NotFound("Recurring transaction was not found.");
        }

        item.Deactivate();
        await Db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Gera as transações das recorrências vencidas. Idempotente: cada execução produz no máximo
    /// uma transação por recorrência por período, sem criar registros futuros em excesso.
    /// </summary>
    public async Task<Result<int>> ExecuteDueAsync(CancellationToken ct = default)
    {
        var today = Time.UtcNow.Date;
        var due = await Db.RecurringTransactions
            .Where(r => r.IsActive && r.NextExecution != null && r.NextExecution.Value.Date <= today)
            .ToListAsync(ct);

        var accounts = await Db.Accounts.ToDictionaryAsync(a => a.Id, ct);
        var created = 0;

        foreach (var recurring in due)
        {
            if (!accounts.TryGetValue(recurring.AccountId, out var account))
            {
                continue;
            }

            var type = recurring.Type;
            var createResult = type == Domain.Enums.TransactionType.Income
                ? Transaction.CreateIncome(recurring.UserId, account.Id, recurring.Amount, recurring.Description, recurring.NextExecution!.Value, recurring.CategoryId)
                : Transaction.CreateExpense(recurring.UserId, account.Id, recurring.Amount, recurring.Description, recurring.NextExecution!.Value, recurring.CategoryId);

            if (createResult.IsSuccess)
            {
                var transaction = createResult.Value!;
                // Rechecagem para não gerar transação duplicada em chamadas concorrentes.
                var exists = await Db.Transactions.AnyAsync(t =>
                    t.UserId == recurring.UserId &&
                    t.Description == recurring.Description &&
                    t.TransactionDate == recurring.NextExecution!.Value.Date &&
                    t.Amount.Amount == recurring.Amount.Amount, ct);
                if (!exists)
                {
                    Db.Transactions.Add(transaction);
                    recurring.AdvanceExecution();
                    created++;
                    await _balance.RecomputeAsync(account, ct);
                }
            }
        }

        if (created > 0)
        {
            await Db.SaveChangesAsync(ct);
        }

        return Result<int>.Success(created);
    }
}

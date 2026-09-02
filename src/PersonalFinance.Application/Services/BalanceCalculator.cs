using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

/// <summary>
/// Centraliza o cálculo de saldo das contas. Garante que o saldo seja sempre derivado
/// das movimentações, nunca alterado de forma arbitrária.
/// </summary>
public sealed class BalanceCalculator
{
    private readonly IAppDbContext _db;

    public BalanceCalculator(IAppDbContext db) => _db = db;

    /// <summary>Transações e transferências que afetam apenas a conta informada.</summary>
    public async Task<IReadOnlyList<(Transaction Tx, Transfer? Transfer)>> GetMovementsAsync(Guid accountId, CancellationToken ct)
    {
        var transactions = await _db.Transactions
            .Where(t => t.AccountId == accountId && t.Status != TransactionStatus.Canceled && !t.TransferId.HasValue)
            .ToListAsync(ct);
        var transfers = await _db.Transfers
            .Where(t => (t.SourceAccountId == accountId || t.TargetAccountId == accountId) && !t.IsCanceled)
            .ToListAsync(ct);

        var result = new List<(Transaction, Transfer?)>();
        foreach (var tx in transactions)
        {
            result.Add((tx, null));
        }

        foreach (var transfer in transfers)
        {
            result.Add((null!, transfer));
        }

        return result;
    }

    /// <summary>Calcula o saldo da conta a partir do saldo inicial e das movimentações.</summary>
    public static Money ComputeBalance(Account account, IEnumerable<(Transaction Tx, Transfer? Transfer)> movements)
    {
        var balance = account.InitialBalance;
        foreach (var (tx, transfer) in movements)
        {
            balance = transfer is not null
                ? balance.Add(transfer.GetNetAmountForAccount(account.Id))
                : balance.Add(tx.GetNetAmountForAccount(account.Id));
        }

        return balance;
    }

    /// <summary>Recalcula e persiste o saldo da conta informada.</summary>
    public async Task RecomputeAsync(Account account, CancellationToken ct)
    {
        var movements = await GetMovementsAsync(account.Id, ct);
        account.SetBalance(ComputeBalance(account, movements));
    }
}

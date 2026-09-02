using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Conta financeira (corrente, poupança, carteira, investimento, outras).
/// O saldo é derivado do saldo inicial + movimentações; não é alterado de forma arbitrária.
/// </summary>
public class Account : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public Money InitialBalance { get; private set; } = Money.Zero();
    public Money CurrentBalance { get; private set; } = Money.Zero();
    public string Currency => CurrentBalance.Currency;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private readonly List<Transfer> _incomingTransfers = new();
    private readonly List<Transfer> _outgoingTransfers = new();

    private Account() { } // EF Core

    public static Result<Account> Create(Guid userId, string name, AccountType type, Money initialBalance, string? currency = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Account name is required.");
        }

        var account = new Account
        {
            UserId = userId,
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance,
            CurrentBalance = initialBalance,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        return Result<Account>.Success(account);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Account name is required.");
        }

        Name = name.Trim();
        Touch();
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>Registra uma movimentação de receita, aumentando o saldo atual.</summary>
    internal void Credit(Money amount)
    {
        CurrentBalance = CurrentBalance.Add(amount);
        Touch();
    }

    /// <summary>Registra uma movimentação de despesa, reduzindo o saldo atual.</summary>
    internal void Debit(Money amount)
    {
        CurrentBalance = CurrentBalance.Subtract(amount);
        Touch();
    }

    /// <summary>Recalcula o saldo a partir do saldo inicial, transações e transferências não canceladas.</summary>
    public void RecalculateBalance(IEnumerable<Transaction> transactions, IEnumerable<Transfer> transfers)
    {
        var balance = InitialBalance;
        foreach (var tx in transactions.Where(t => t.Status != TransactionStatus.Canceled && !t.TransferId.HasValue))
        {
            balance = balance.Add(tx.GetNetAmountForAccount(Id));
        }

        foreach (var transfer in transfers.Where(t => !t.IsCanceled))
        {
            balance = balance.Add(transfer.GetNetAmountForAccount(Id));
        }

        CurrentBalance = balance;
        Touch();
    }

    public void SetBalance(Money balance)
    {
        CurrentBalance = balance;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    public void IncrementVersion() => Version++;

    public void BumpVersion() => Version++;
}

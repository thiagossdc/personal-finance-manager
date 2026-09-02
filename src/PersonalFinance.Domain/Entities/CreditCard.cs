using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Cartão de crédito com limite, data de fechamento e vencimento da fatura, e histórico de compras.
/// </summary>
public class CreditCard : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? LastFourDigits { get; private set; }
    public Money CreditLimit { get; private set; } = Money.Zero();
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public CreditCardStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private readonly List<CreditCardTransaction> _transactions = new();
    public IReadOnlyCollection<CreditCardTransaction> Transactions => _transactions.AsReadOnly();

    private CreditCard() { } // EF Core

    public static Result<CreditCard> Create(
        Guid userId,
        string name,
        Money creditLimit,
        int closingDay,
        int dueDay,
        string? lastFourDigits = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Credit card name is required.");
        }

        if (closingDay is < 1 or > 31)
        {
            return Error.Validation("Closing day must be between 1 and 31.");
        }

        if (dueDay is < 1 or > 31)
        {
            return Error.Validation("Due day must be between 1 and 31.");
        }

        var now = DateTime.UtcNow;
        return Result<CreditCard>.Success(new CreditCard
        {
            UserId = userId,
            Name = name.Trim(),
            LastFourDigits = lastFourDigits,
            CreditLimit = creditLimit,
            ClosingDay = closingDay,
            DueDay = dueDay,
            Status = CreditCardStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    /// <summary>Total ainda aberto na fatura: compras à vista não pagas + parcelas abertas.</summary>
    public Money CurrentOpenAmount()
    {
        var total = Money.Zero(CreditLimit.Currency);
        foreach (var purchase in _transactions.Where(t => t.Status != PaymentStatus.Canceled))
        {
            if (purchase.InstallmentCount <= 1)
            {
                if (purchase.Status != PaymentStatus.Paid)
                {
                    total = total.Add(purchase.Amount);
                }
            }
            else
            {
                var open = purchase.Installments
                    .Where(i => i.Status != PaymentStatus.Paid && i.Status != PaymentStatus.Canceled)
                    .Aggregate(Money.Zero(CreditLimit.Currency), (acc, i) => acc.Add(i.Amount));
                total = total.Add(open);
            }
        }

        return total;
    }

    public Money AvailableLimit() => CreditLimit.Subtract(CurrentOpenAmount());

    /// <summary>Percentual de utilização do limite (0-1).</summary>
    public decimal UtilizationRatio() => CreditLimit.Amount == 0m ? 0m : Math.Abs(CurrentOpenAmount().Amount) / CreditLimit.Amount;

    public void AddPurchase(CreditCardTransaction purchase)
    {
        _transactions.Add(purchase);
        Touch();
        IncrementVersion();
    }

    internal void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    public void IncrementVersion() => Version++;

    public void BumpVersion() => Version++;
}

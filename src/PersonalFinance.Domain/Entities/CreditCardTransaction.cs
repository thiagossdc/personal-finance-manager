using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Compra no cartão de crédito. Pode ser à vista ou parcelada (gerando <see cref="Installment"/>).
/// </summary>
public class CreditCardTransaction : Entity
{
    public Guid UserId { get; private set; }
    public Guid CreditCardId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime PurchaseDate { get; private set; }
    public int InstallmentCount { get; private set; }
    public Guid? CategoryId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<Installment> _installments = new();
    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    private CreditCardTransaction() { } // EF Core

    public static Result<CreditCardTransaction> Create(
        Guid userId,
        Guid creditCardId,
        string description,
        Money amount,
        DateTime purchaseDate,
        int installmentCount = 1,
        Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Description is required.");
        }

        if (installmentCount < 1)
        {
            return Error.Validation("Installment count must be at least 1.");
        }

        return Result<CreditCardTransaction>.Success(new CreditCardTransaction
        {
            UserId = userId,
            CreditCardId = creditCardId,
            Description = description.Trim(),
            Amount = Money.From(Math.Abs(amount.Amount), amount.Currency),
            PurchaseDate = purchaseDate.Date,
            InstallmentCount = installmentCount,
            CategoryId = categoryId,
            Status = installmentCount > 1 ? PaymentStatus.Pending : PaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public void AddInstallment(Installment installment) => _installments.Add(installment);

    public void MarkPaid()
    {
        Status = PaymentStatus.Paid;
        foreach (var i in _installments)
        {
            i.MarkPaid();
        }
    }
}

/// <summary>
/// Parcela de uma compra parcelada, com número, total, vencimento, status e vínculo com a compra original.
/// </summary>
public class Installment : Entity
{
    public Guid UserId { get; private set; }
    public Guid CreditCardTransactionId { get; private set; }
    public int Number { get; private set; }
    public int Total { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime DueDate { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    private Installment() { } // EF Core

    public static Result<Installment> Create(
        Guid userId,
        Guid creditCardTransactionId,
        int number,
        int total,
        Money amount,
        DateTime dueDate)
    {
        if (number < 1 || total < 1 || number > total)
        {
            return Error.Validation("Invalid installment number/total.");
        }

        return Result<Installment>.Success(new Installment
        {
            UserId = userId,
            CreditCardTransactionId = creditCardTransactionId,
            Number = number,
            Total = total,
            Amount = Money.From(Math.Abs(amount.Amount), amount.Currency),
            DueDate = dueDate.Date,
            Status = PaymentStatus.Pending
        });
    }

    public void MarkPaid()
    {
        Status = PaymentStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
    }

    public bool IsOverdue(DateTime today) => Status == PaymentStatus.Pending && DueDate.Date < today.Date;
}

using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Transferência entre contas. Modelada como uma relação explícita e atômica: debita a conta
/// origem e credita a conta destino na mesma operação, garantindo consistência.
/// </summary>
public class Transfer : Entity
{
    public Guid UserId { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Guid TargetAccountId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime TransferDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsCanceled { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Transfer() { } // EF Core

    public static Result<Transfer> Create(
        Guid? userId,
        Guid sourceAccountId,
        Guid targetAccountId,
        Money amount,
        DateTime date,
        string? description = null)
    {
        if (sourceAccountId == targetAccountId)
        {
            return Error.Validation("Source and target account must differ.");
        }

        if (amount.Amount <= 0m)
        {
            return Error.Validation("Transfer amount must be positive.");
        }

        var now = DateTime.UtcNow;
        return Result<Transfer>.Success(new Transfer
        {
            UserId = userId ?? Guid.Empty,
            SourceAccountId = sourceAccountId,
            TargetAccountId = targetAccountId,
            Amount = Money.From(Math.Abs(amount.Amount), amount.Currency),
            TransferDate = date.Date,
            Description = string.IsNullOrWhiteSpace(description) ? "Transfer" : description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    public Money GetNetAmountForAccount(Guid accountId)
    {
        if (accountId == SourceAccountId)
        {
            return Money.From(-Math.Abs(Amount.Amount), Amount.Currency);
        }

        if (accountId == TargetAccountId)
        {
            return Amount;
        }

        return Money.Zero(Amount.Currency);
    }

    public void Cancel()
    {
        IsCanceled = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

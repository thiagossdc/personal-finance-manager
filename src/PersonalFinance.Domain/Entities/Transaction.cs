using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Movimentação financeira (receita ou despesa). As transferências possuem relação explícita
/// própria (entidade <see cref="Transfer"/>), não sendo modeladas como duas transações soltas.
/// </summary>
public class Transaction : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? TransferId { get; private set; }
    public Guid? InstallmentId { get; private set; }
    public Guid? RecurringTransactionId { get; private set; }
    public Guid? FinancialGoalId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public string Description { get; private set; } = string.Empty;
    public DateTime TransactionDate { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string? Note { get; private set; }
    public Guid? ParentId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private readonly List<Tag> _tags = new();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private readonly List<Attachment> _attachments = new();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    public Account? Account { get; private set; }
    public Category? Category { get; private set; }
    public Transfer? Transfer { get; private set; }

    private Transaction() { } // EF Core

    public static Result<Transaction> CreateIncome(
        Guid userId,
        Guid accountId,
        Money amount,
        string description,
        DateTime date,
        Guid? categoryId = null,
        string? note = null)
    {
        return CreateTransaction(userId, accountId, TransactionType.Income, amount, description, date, categoryId, note, parentId: null);
    }

    public static Result<Transaction> CreateExpense(
        Guid userId,
        Guid accountId,
        Money amount,
        string description,
        DateTime date,
        Guid? categoryId = null,
        string? note = null)
    {
        return CreateTransaction(userId, accountId, TransactionType.Expense, amount, description, date, categoryId, note, parentId: null);
    }

    internal static Result<Transaction> CreateTransaction(
        Guid userId,
        Guid accountId,
        TransactionType type,
        Money amount,
        string description,
        DateTime date,
        Guid? categoryId,
        string? note,
        Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Transaction description is required.");
        }

        if (amount.Amount == 0m)
        {
            return Error.Validation("Transaction amount cannot be zero.");
        }

        var now = DateTime.UtcNow;
        var t = new Transaction
        {
            UserId = userId,
            AccountId = accountId,
            Type = type,
            Amount = Money.From(Math.Abs(amount.Amount), amount.Currency),
            Description = description.Trim(),
            TransactionDate = date.Date,
            CategoryId = categoryId,
            Note = note,
            ParentId = parentId,
            Status = TransactionStatus.Confirmed,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        return Result<Transaction>.Success(t);
    }

    /// <summary>Define o vínculo com a transferência que originou esta movimentação.</summary>
    internal void LinkToTransfer(Guid transferId)
    {
        TransferId = transferId;
        Touch();
    }

    internal void LinkToInstallment(Guid installmentId)
    {
        InstallmentId = installmentId;
        Touch();
    }

    /// <summary>Sinal de crédito/débito da transação na conta informada (para cálculo de saldo).</summary>
    public Money GetNetAmountForAccount(Guid accountId)
    {
        if (accountId == AccountId)
        {
            return Type == TransactionType.Income
                ? Amount
                : Money.From(-Math.Abs(Amount.Amount), Amount.Currency);
        }

        return Money.Zero(Amount.Currency);
    }

    public Result Update(DateTime date, Money amount, string description, Guid? categoryId, string? note)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Transaction description is required.");
        }

        if (amount.Amount <= 0m)
        {
            return Error.Validation("Transaction amount must be positive.");
        }

        TransactionDate = date.Date;
        Amount = Money.From(Math.Abs(amount.Amount), amount.Currency);
        Description = description.Trim();
        CategoryId = categoryId;
        Note = note;
        Touch();
        return Result.Success();
    }

    public void MarkCanceled()
    {
        Status = TransactionStatus.Canceled;
        Touch();
    }

    public void MarkConfirmed()
    {
        Status = TransactionStatus.Confirmed;
        Touch();
    }

    public void AddTag(Tag tag)
    {
        if (_tags.All(t => t.Name != tag.Name))
        {
            _tags.Add(tag);
            Touch();
        }
    }

    public void AddAttachment(Attachment attachment)
    {
        _attachments.Add(attachment);
        Touch();
    }

    public void RemoveAttachment(Attachment attachment) => _attachments.Remove(attachment);

    internal void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    public void IncrementVersion() => Version++;

    public void BumpVersion() => Version++;
}

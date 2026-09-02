using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void CreateIncome_WithValidData_Succeeds()
    {
        var result = Transaction.CreateIncome(
            UserId, AccountId, Money.From(5000m), "Salário", DateTime.UtcNow, CategoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(TransactionType.Income);
        result.Value.Status.Should().Be(TransactionStatus.Confirmed);
    }

    [Fact]
    public void CreateExpense_WithValidData_Succeeds()
    {
        var result = Transaction.CreateExpense(
            UserId, AccountId, Money.From(150m), "Supermercado", DateTime.UtcNow, CategoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(TransactionType.Expense);
    }

    [Fact]
    public void Create_WithEmptyDescription_Fails()
    {
        var result = Transaction.CreateIncome(UserId, AccountId, Money.From(100m), "  ", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithZeroAmount_Fails()
    {
        var result = Transaction.CreateIncome(UserId, AccountId, Money.Zero(), "Test", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void GetNetAmountForAccount_Income_IsPositive()
    {
        var tx = Transaction.CreateIncome(UserId, AccountId, Money.From(100m), "Test", DateTime.UtcNow).Value!;

        tx.GetNetAmountForAccount(AccountId).Amount.Should().Be(100m);
    }

    [Fact]
    public void GetNetAmountForAccount_Expense_IsNegative()
    {
        var tx = Transaction.CreateExpense(UserId, AccountId, Money.From(100m), "Test", DateTime.UtcNow).Value!;

        tx.GetNetAmountForAccount(AccountId).Amount.Should().Be(-100m);
    }

    [Fact]
    public void Update_WithValidData_Succeeds()
    {
        var tx = Transaction.CreateExpense(UserId, AccountId, Money.From(100m), "Old", DateTime.UtcNow).Value!;
        var newDate = DateTime.UtcNow.AddDays(1);

        var result = tx.Update(newDate, Money.From(200m), "New", CategoryId, "note");

        result.IsSuccess.Should().BeTrue();
        tx.Description.Should().Be("New");
        tx.Amount.Amount.Should().Be(200m);
    }

    [Fact]
    public void MarkCanceled_ChangesStatus()
    {
        var tx = Transaction.CreateIncome(UserId, AccountId, Money.From(100m), "Test", DateTime.UtcNow).Value!;

        tx.MarkCanceled();

        tx.Status.Should().Be(TransactionStatus.Canceled);
    }
}

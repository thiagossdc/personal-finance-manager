using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public class AccountBalanceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void RecalculateBalance_WithIncomeAndExpense_ComputesCorrectly()
    {
        var account = Account.Create(UserId, "Conta", AccountType.Checking, Money.From(1000m)).Value!;
        var income = Transaction.CreateIncome(UserId, account.Id, Money.From(500m), "Salário", DateTime.UtcNow).Value!;
        var expense = Transaction.CreateExpense(UserId, account.Id, Money.From(200m), "Compra", DateTime.UtcNow).Value!;

        account.RecalculateBalance([income, expense], []);

        account.CurrentBalance.Amount.Should().Be(1300m);
    }

    [Fact]
    public void RecalculateBalance_WithTransfer_ComputesCorrectly()
    {
        var account = Account.Create(UserId, "Conta", AccountType.Checking, Money.From(1000m)).Value!;
        var otherId = Guid.NewGuid();
        var transfer = Transfer.Create(UserId, account.Id, otherId, Money.From(300m), DateTime.UtcNow).Value!;

        account.RecalculateBalance([], [transfer]);

        account.CurrentBalance.Amount.Should().Be(700m);
    }

    [Fact]
    public void RecalculateBalance_IgnoresCanceledTransactions()
    {
        var account = Account.Create(UserId, "Conta", AccountType.Checking, Money.From(1000m)).Value!;
        var tx = Transaction.CreateExpense(UserId, account.Id, Money.From(200m), "Cancelada", DateTime.UtcNow).Value!;
        tx.MarkCanceled();

        account.RecalculateBalance([tx], []);

        account.CurrentBalance.Amount.Should().Be(1000m);
    }

    [Fact]
    public void CreditAndDebit_UpdateBalance()
    {
        var account = Account.Create(UserId, "Conta", AccountType.Wallet, Money.From(100m)).Value!;

        account.Credit(Money.From(50m));
        account.Debit(Money.From(30m));

        account.CurrentBalance.Amount.Should().Be(120m);
    }
}

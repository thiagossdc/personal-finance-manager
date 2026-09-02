using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public sealed class AccountTests
{
    [Fact]
    public void Create_ShouldInitializeWithInitialBalance()
    {
        var userId = Guid.NewGuid();
        var initialBalance = Money.Create(1000m, "BRL").Value!;

        var result = Account.Create(userId, "Conta Corrente", AccountType.Checking, initialBalance);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Conta Corrente");
        result.Value.Type.Should().Be(AccountType.Checking);
        result.Value.InitialBalance.Amount.Should().Be(1000m);
        result.Value.CurrentBalance.Amount.Should().Be(1000m);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        var result = Account.Create(Guid.NewGuid(), "", AccountType.Checking, Money.Create(100m).Value!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Rename_ShouldUpdateName()
    {
        var account = Account.Create(Guid.NewGuid(), "Nome Original", AccountType.Checking, Money.Create(100m).Value!).Value!;

        var result = account.Rename("Novo Nome");

        result.IsSuccess.Should().BeTrue();
        account.Name.Should().Be("Novo Nome");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var account = Account.Create(Guid.NewGuid(), "Conta", AccountType.Checking, Money.Create(100m).Value!).Value!;

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RecalculateBalance_WithIncomeAndExpense_ShouldCalculateCorrectly()
    {
        var account = Account.Create(Guid.NewGuid(), "Conta", AccountType.Checking, Money.Create(1000m).Value!).Value!;

        var income = Transaction.CreateIncome(Guid.NewGuid(), account.Id, Money.Create(500m).Value!, "Receita", DateTime.UtcNow).Value!;
        var expense = Transaction.CreateExpense(Guid.NewGuid(), account.Id, Money.Create(200m).Value!, "Despesa", DateTime.UtcNow).Value!;

        account.RecalculateBalance([income, expense], []);

        account.CurrentBalance.Amount.Should().Be(1300m); // 1000 + 500 - 200
    }

    [Fact]
    public void RecalculateBalance_ShouldIgnoreCanceledTransactions()
    {
        var account = Account.Create(Guid.NewGuid(), "Conta", AccountType.Checking, Money.Create(1000m).Value!).Value!;

        var income = Transaction.CreateIncome(Guid.NewGuid(), account.Id, Money.Create(500m).Value!, "Receita", DateTime.UtcNow).Value!;
        var canceledExpense = Transaction.CreateExpense(Guid.NewGuid(), account.Id, Money.Create(200m).Value!, "Despesa Cancelada", DateTime.UtcNow).Value!;
        canceledExpense.MarkCanceled();

        account.RecalculateBalance([income, canceledExpense], []);

        account.CurrentBalance.Amount.Should().Be(1500m); // 1000 + 500 (canceled ignored)
    }

    [Fact]
    public void RecalculateBalance_WithTransfer_ShouldAdjustBothSides()
    {
        var accountA = Account.Create(Guid.NewGuid(), "Conta A", AccountType.Checking, Money.Create(1000m).Value!).Value!;
        var accountB = Account.Create(Guid.NewGuid(), "Conta B", AccountType.Checking, Money.Create(500m).Value!).Value!;

        var transfer = Transfer.Create(null, accountA.Id, accountB.Id, Money.Create(300m).Value!, DateTime.UtcNow).Value!;

        accountA.RecalculateBalance([], [transfer]);
        accountB.RecalculateBalance([], [transfer]);

        accountA.CurrentBalance.Amount.Should().Be(700m); // 1000 - 300
        accountB.CurrentBalance.Amount.Should().Be(800m); // 500 + 300
    }
}

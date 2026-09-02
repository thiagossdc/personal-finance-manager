using FluentAssertions;
using Moq;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Services;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Tests;

public class BalanceCalculatorTests
{
    [Fact]
    public void ComputeBalance_WithMixedMovements_ReturnsCorrectTotal()
    {
        var userId = Guid.NewGuid();
        var account = Account.Create(userId, "Conta", AccountType.Checking, Money.From(1000m)).Value!;
        var income = Transaction.CreateIncome(userId, account.Id, Money.From(500m), "Salário", DateTime.UtcNow).Value!;
        var expense = Transaction.CreateExpense(userId, account.Id, Money.From(200m), "Compra", DateTime.UtcNow).Value!;
        var otherId = Guid.NewGuid();
        var transfer = Transfer.Create(userId, account.Id, otherId, Money.From(100m), DateTime.UtcNow).Value!;

        var movements = new List<(Transaction Tx, Transfer? Transfer)>
        {
            (income, null),
            (expense, null),
            (null!, transfer)
        };

        var balance = BalanceCalculator.ComputeBalance(account, movements);

        balance.Amount.Should().Be(1200m);
    }
}

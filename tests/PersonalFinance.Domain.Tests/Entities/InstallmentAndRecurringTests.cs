using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public class InstallmentAndRecurringTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Installment_Create_WithValidData_Succeeds()
    {
        var result = Installment.Create(UserId, Guid.NewGuid(), 1, 12, Money.From(300m), DateTime.UtcNow.AddMonths(1));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(1);
        result.Value.Total.Should().Be(12);
    }

    [Fact]
    public void Installment_Create_InvalidNumber_Fails()
    {
        Installment.Create(UserId, Guid.NewGuid(), 13, 12, Money.From(300m), DateTime.UtcNow).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Installment_IsOverdue_WhenPastDueAndPending()
    {
        var installment = Installment.Create(UserId, Guid.NewGuid(), 1, 3, Money.From(100m), DateTime.UtcNow.AddDays(-5)).Value!;

        installment.IsOverdue(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CreditCardTransaction_Create_WithInstallments_Succeeds()
    {
        var result = CreditCardTransaction.Create(UserId, Guid.NewGuid(), "Notebook", Money.From(3600m), DateTime.UtcNow, 12);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InstallmentCount.Should().Be(12);
    }

    [Fact]
    public void RecurringTransaction_CalculateNext_Monthly_AdvancesOneMonth()
    {
        var start = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var next = RecurringTransaction.CalculateNext(start, start, RecurrenceFrequency.Monthly, 1);

        next.Should().Be(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void RecurringTransaction_IsDue_WhenNextExecutionReached()
    {
        var recurring = RecurringTransaction.Create(
            UserId, Guid.NewGuid(), TransactionType.Expense, Money.From(50m),
            "Netflix", RecurrenceFrequency.Monthly, DateTime.UtcNow.AddMonths(-1)).Value!;

        recurring.IsDue(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void RecurringTransaction_AdvanceExecution_UpdatesNextExecution()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurring = RecurringTransaction.Create(
            UserId, Guid.NewGuid(), TransactionType.Expense, Money.From(50m),
            "Netflix", RecurrenceFrequency.Monthly, start).Value!;

        recurring.AdvanceExecution();

        recurring.LastExecutedAt.Should().NotBeNull();
        recurring.NextExecution.Should().BeAfter(start);
    }
}

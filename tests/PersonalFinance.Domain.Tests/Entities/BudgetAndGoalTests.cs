using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public class BudgetAndGoalTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Budget_Progress_CalculatesCorrectly()
    {
        var budget = Budget.Create(UserId, CategoryId, Money.From(1000m), BudgetPeriod.Monthly, 8, 2026).Value!;
        budget.SetSpent(Money.From(720m));

        budget.Progress().Should().Be(0.72m);
        budget.Remaining().Amount.Should().Be(280m);
        budget.IsAlertTriggered().Should().BeFalse();
    }

    [Fact]
    public void Budget_AlertTriggered_At80Percent()
    {
        var budget = Budget.Create(UserId, CategoryId, Money.From(1000m), BudgetPeriod.Monthly, alertThreshold: 0.8m).Value!;
        budget.SetSpent(Money.From(800m));

        budget.IsAlertTriggered().Should().BeTrue();
    }

    [Fact]
    public void Budget_Create_WithInvalidLimit_Fails()
    {
        Budget.Create(UserId, CategoryId, Money.Zero(), BudgetPeriod.Monthly).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void FinancialGoal_Contribute_UpdatesProgress()
    {
        var goal = FinancialGoal.Create(UserId, "Fundo Emergência", Money.From(10000m)).Value!;

        goal.Contribute(Money.From(6500m)).IsSuccess.Should().BeTrue();

        goal.Progress().Should().Be(0.65m);
        goal.Remaining().Amount.Should().Be(3500m);
        goal.Status.Should().Be(GoalStatus.Active);
    }

    [Fact]
    public void FinancialGoal_Contribute_ReachingTarget_MarksAchieved()
    {
        var goal = FinancialGoal.Create(UserId, "Meta", Money.From(1000m)).Value!;

        goal.Contribute(Money.From(1000m));

        goal.Status.Should().Be(GoalStatus.Achieved);
    }
}

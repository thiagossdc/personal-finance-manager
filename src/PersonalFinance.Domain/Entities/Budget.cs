using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Orçamento por categoria com limite, período e alertas configuráveis (80% e 100%).
/// </summary>
public class Budget : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Money Limit { get; private set; } = Money.Zero();
    public BudgetPeriod Period { get; private set; }
    public int? Month { get; private set; }
    public int? Year { get; private set; }
    public decimal AlertThreshold { get; private set; } = 0.8m;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private Budget() { } // EF Core

    public static Result<Budget> Create(Guid userId, Guid categoryId, Money limit, BudgetPeriod period, int? month = null, int? year = null, decimal alertThreshold = 0.8m)
    {
        if (limit.Amount <= 0m)
        {
            return Error.Validation("Budget limit must be positive.");
        }

        if (alertThreshold is < 0m or > 1m)
        {
            return Error.Validation("Alert threshold must be between 0 and 1.");
        }

        if (period == BudgetPeriod.Monthly && (month is < 1 or > 12))
        {
            return Error.Validation("Month must be between 1 and 12 for monthly budgets.");
        }

        var now = DateTime.UtcNow;
        var effectiveYear = year ?? now.Year;
        var effectiveMonth = month ?? (period == BudgetPeriod.Monthly ? now.Month : (int?)null);

        return Result<Budget>.Success(new Budget
        {
            UserId = userId,
            CategoryId = categoryId,
            Limit = limit,
            Period = period,
            Month = effectiveMonth,
            Year = effectiveYear,
            AlertThreshold = alertThreshold,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1
        });
    }

    public void BumpVersion() => Version++;
    public override void AssignClientId(Guid id) => Id = id;

    public Result UpdateLimit(Money newLimit)
    {
        if (newLimit.Amount <= 0m)
        {
            return Error.Validation("Budget limit must be positive.");
        }

        Limit = newLimit;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void SetAlertThreshold(decimal threshold)
    {
        AlertThreshold = threshold;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Contribuição do gasto das transações da categoria no período.</summary>
    public void SetSpent(Money spent) => Spent = spent;

    public Money Spent { get; private set; } = Money.Zero();

    public Money Remaining() => Limit.Subtract(Spent);

    public decimal Progress() => Money.Percentage(Spent, Limit);

    public bool IsAlertTriggered() => Progress() >= AlertThreshold;
}

/// <summary>
/// Meta financeira (ex.: fundo de emergência) com valor alvo, prazo e histórico de contribuições.
/// </summary>
public class FinancialGoal : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money TargetAmount { get; private set; } = Money.Zero();
    public Money CurrentAmount { get; private set; } = Money.Zero();
    public DateTime? Deadline { get; private set; }
    public GoalStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private readonly List<GoalContribution> _contributions = new();
    public IReadOnlyCollection<GoalContribution> Contributions => _contributions.AsReadOnly();

    private FinancialGoal() { } // EF Core

    public static Result<FinancialGoal> Create(Guid userId, string name, Money targetAmount, DateTime? deadline = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Goal name is required.");
        }

        if (targetAmount.Amount <= 0m)
        {
            return Error.Validation("Target amount must be positive.");
        }

        var now = DateTime.UtcNow;
        return Result<FinancialGoal>.Success(new FinancialGoal
        {
            UserId = userId,
            Name = name.Trim(),
            TargetAmount = targetAmount,
            Deadline = deadline,
            Status = GoalStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1
        });
    }

    public void BumpVersion() => Version++;
    public override void AssignClientId(Guid id) => Id = id;

    public void Cancel()
    {
        Status = GoalStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Result Contribute(Money amount, string? note = null)
    {
        if (amount.Amount <= 0m)
        {
            return Error.Validation("Contribution must be positive.");
        }

        CurrentAmount = CurrentAmount.Add(amount);
        _contributions.Add(new GoalContribution
        {
            Amount = amount,
            Note = note,
            ContributedAtUtc = DateTime.UtcNow
        });
        UpdatedAtUtc = DateTime.UtcNow;

        if (CurrentAmount >= TargetAmount)
        {
            Status = GoalStatus.Achieved;
        }

        return Result.Success();
    }

    public decimal Progress() => Money.Percentage(CurrentAmount, TargetAmount);

    public Money Remaining() => TargetAmount.Subtract(CurrentAmount);
}

public class GoalContribution
{
    public Money Amount { get; internal set; } = Money.Zero();
    public string? Note { get; internal set; }
    public DateTime ContributedAtUtc { get; internal set; }
}

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Services;

public sealed class DashboardService : ServiceBase, IDashboardService
{
    public DashboardService(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
        : base(db, currentUser, time)
    {
    }

    public async Task<Result<DashboardDto>> GetDashboardAsync(int? month = null, int? year = null, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var now = Time.UtcNow;
        var effectiveMonth = month ?? now.Month;
        var effectiveYear = year ?? now.Year;

        var totalBalance = await Db.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .SumAsync(a => (decimal?)a.CurrentBalance.Amount, ct) ?? 0m;

        var monthIncome = await MonthlySumAsync(userId, TransactionType.Income, effectiveMonth, effectiveYear, ct);
        var monthExpense = await MonthlySumAsync(userId, TransactionType.Expense, effectiveMonth, effectiveYear, ct);
        var monthlySavings = monthIncome - monthExpense;

        // Receitas x Despesas dos últimos 6 meses
        var incomeSeries = new List<SeriesPoint>();
        var expenseSeries = new List<SeriesPoint>();
        var netWorthSeries = new List<SeriesPoint>();
        var runningNetWorth = decimal.Zero;
        for (var i = 5; i >= 0; i--)
        {
            var d = new DateTime(effectiveYear, effectiveMonth, 1).AddMonths(-i);
            var income = await MonthlySumAsync(userId, TransactionType.Income, d.Month, d.Year, ct);
            var expense = await MonthlySumAsync(userId, TransactionType.Expense, d.Month, d.Year, ct);
            runningNetWorth += income - expense;
            var label = d.ToString("MMM/yy", CultureInfo.GetCultureInfo("pt-BR"));
            incomeSeries.Add(new SeriesPoint(label, income));
            expenseSeries.Add(new SeriesPoint(label, expense));
            netWorthSeries.Add(new SeriesPoint(label, runningNetWorth));
        }

        // Despesas por categoria
        var expenseByCategory = await Db.Transactions
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.Status != TransactionStatus.Canceled
                        && t.TransactionDate.Year == effectiveYear && t.TransactionDate.Month == effectiveMonth && t.CategoryId != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new CategorySlice(g.Key, g.Sum(t => t.Amount.Amount)))
            .ToListAsync(ct);

        // Orçamentos
        var budgets = await Db.Budgets
            .Where(b => b.UserId == userId)
            .ToListAsync(ct);
        var budgetDto = new List<BudgetDto>();
        foreach (var b in budgets)
        {
            var spent = await Db.Transactions
                .Where(t => t.UserId == userId && t.CategoryId == b.CategoryId && t.Type == TransactionType.Expense
                            && t.Status != TransactionStatus.Canceled && t.TransactionDate.Year == effectiveYear
                            && (b.Period == BudgetPeriod.Yearly || t.TransactionDate.Month == effectiveMonth))
                .SumAsync(t => (decimal?)t.Amount.Amount, ct) ?? 0m;
            b.SetSpent(Domain.ValueObjects.Money.From(spent, b.Limit.Currency));
            budgetDto.Add(b.ToDto(categoryName: string.Empty));
        }

        // Metas
        var goals = await Db.FinancialGoals.Where(g => g.UserId == userId).ToListAsync(ct);

        // Transações recentes
        var recent = await Db.Transactions.Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(8)
            .ToListAsync(ct);

        var result = new DashboardDto(
            CurrentBalance: totalBalance,
            TotalIncome: monthIncome,
            TotalExpense: monthExpense,
            TotalSavings: monthlySavings,
            Month: effectiveMonth,
            Year: effectiveYear,
            RecentTransactions: recent.Select(t => t.ToDto(string.Empty)).ToList(),
            BudgetStatus: budgetDto,
            Goals: goals.Select(g => g.ToDto()).ToList(),
            IncomeSeries: incomeSeries,
            ExpenseSeries: expenseSeries,
            ExpenseByCategory: expenseByCategory,
            NetWorthSeries: netWorthSeries);

        return Result<DashboardDto>.Success(result);
    }

    private async Task<decimal> MonthlySumAsync(Guid userId, TransactionType type, int month, int year, CancellationToken ct)
    {
        return await Db.Transactions
            .Where(t => t.UserId == userId && t.Type == type && t.Status != TransactionStatus.Canceled
                        && t.TransactionDate.Year == year && t.TransactionDate.Month == month)
            .SumAsync(t => (decimal?)t.Amount.Amount, ct) ?? 0m;
    }
}

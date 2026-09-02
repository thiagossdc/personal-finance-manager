using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed class BudgetService : ServiceBase, IBudgetService
{
    public BudgetService(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
        : base(db, currentUser, time)
    {
    }

    public async Task<Result<IReadOnlyList<BudgetDto>>> GetBudgetsAsync(int? month = null, int? year = null, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var now = Time.UtcNow;
        var effectiveMonth = month ?? now.Month;
        var effectiveYear = year ?? now.Year;

        var budgets = await Db.Budgets
            .Where(b => b.UserId == userId && (b.Period == BudgetPeriod.Yearly || (b.Month == effectiveMonth && b.Year == effectiveYear)))
            .ToListAsync(ct);

        var categories = await Db.Categories.Where(c => c.UserId == userId).ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        foreach (var budget in budgets)
        {
            var spent = await ComputeSpentAsync(userId, budget, effectiveMonth, effectiveYear, ct);
            budget.SetSpent(spent);
        }

        if (budgets.Count > 0)
        {
            await Db.SaveChangesAsync(ct);
        }

        return Result<IReadOnlyList<BudgetDto>>.Success(budgets.Select(b => b.ToDto(categories.TryGetValue(b.CategoryId, out var name) ? name : string.Empty)).ToList());
    }

    public async Task<Result<BudgetDto>> CreateAsync(CreateBudgetRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        if (!await Db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId, ct))
        {
            return Error.NotFound("Category was not found.");
        }

        var now = Time.UtcNow;
        var createResult = Budget.Create(userId, request.CategoryId, Money.From(request.Limit), request.Period, request.Month, request.Year, request.AlertThreshold);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.Budgets.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);

        var categoryName = await Db.Categories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(ct) ?? string.Empty;
        return Result<BudgetDto>.Success(createResult.Value!.ToDto(categoryName));
    }

    public async Task<Result<BudgetDto>> UpdateLimitAsync(Guid id, decimal limit, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var budget = await Db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, ct);
        if (budget is null)
        {
            return Error.NotFound("Budget was not found.");
        }

        var update = budget.UpdateLimit(Money.From(limit, budget.Limit.Currency));
        if (update.IsFailure)
        {
            return Result<BudgetDto>.Failure(update.Error!);
        }

        await Db.SaveChangesAsync(ct);
        var categoryName = await Db.Categories.Where(c => c.Id == budget.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(ct) ?? string.Empty;
        return Result<BudgetDto>.Success(budget.ToDto(categoryName));
    }

    private async Task<Money> ComputeSpentAsync(Guid userId, Budget budget, int month, int year, CancellationToken ct)
    {
        var query = Db.Transactions.Where(t =>
            t.UserId == userId &&
            t.CategoryId == budget.CategoryId &&
            t.Type == TransactionType.Expense &&
            t.Status != TransactionStatus.Canceled);

        if (budget.Period == BudgetPeriod.Monthly)
        {
            query = query.Where(t => t.TransactionDate.Year == year && t.TransactionDate.Month == month);
        }
        else
        {
            query = query.Where(t => t.TransactionDate.Year == year);
        }

        var sum = await query.SumAsync(t => (decimal?)t.Amount.Amount, ct) ?? 0m;
        return Money.From(sum, budget.Limit.Currency);
    }
}

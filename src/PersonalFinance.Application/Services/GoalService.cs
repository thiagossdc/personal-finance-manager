using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed class GoalService : ServiceBase, IGoalService
{
    public GoalService(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
        : base(db, currentUser, time)
    {
    }

    public async Task<Result<IReadOnlyList<GoalDto>>> GetGoalsAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var goals = await Db.FinancialGoals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Deadline)
            .ToListAsync(ct);
        return Result<IReadOnlyList<GoalDto>>.Success(goals.Select(g => g.ToDto()).ToList());
    }

    public async Task<Result<GoalDto>> CreateAsync(CreateGoalRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var createResult = FinancialGoal.Create(userId, request.Name, Money.From(request.TargetAmount), request.Deadline);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.FinancialGoals.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);
        return Result<GoalDto>.Success(createResult.Value!.ToDto());
    }

    public async Task<Result<GoalDto>> ContributeAsync(Guid id, ContributeGoalRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var goal = await Db.FinancialGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);
        if (goal is null)
        {
            return Error.NotFound("Goal was not found.");
        }

        var contribute = goal.Contribute(Money.From(request.Amount, goal.TargetAmount.Currency), request.Note);
        if (contribute.IsFailure)
        {
            return Result<GoalDto>.Failure(contribute.Error!);
        }

        await Db.SaveChangesAsync(ct);
        return Result<GoalDto>.Success(goal.ToDto());
    }
}

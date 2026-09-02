using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class GoalsController : BaseController
{
    private readonly IGoalService _goalService;

    public GoalsController(ICurrentUser currentUser, IGoalService goalService)
        : base(currentUser)
    {
        _goalService = goalService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GoalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(CancellationToken ct)
    {
        var result = await _goalService.GetGoalsAsync(ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request, CancellationToken ct)
    {
        var result = await _goalService.CreateAsync(request, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/contribute")]
    [ProducesResponseType(typeof(ApiResponse<GoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Contribute(Guid id, [FromBody] ContributeGoalRequest request, CancellationToken ct)
    {
        var result = await _goalService.ContributeAsync(id, request, ct);
        return FromResult(result);
    }
}

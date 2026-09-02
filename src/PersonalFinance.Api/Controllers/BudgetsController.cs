using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class BudgetsController : BaseController
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(ICurrentUser currentUser, IBudgetService budgetService)
        : base(currentUser)
    {
        _budgetService = budgetService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BudgetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgets([FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
    {
        var result = await _budgetService.GetBudgetsAsync(month, year, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request, CancellationToken ct)
    {
        var result = await _budgetService.CreateAsync(request, ct);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/limit")]
    [ProducesResponseType(typeof(ApiResponse<BudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLimit(Guid id, [FromBody] decimal limit, CancellationToken ct)
    {
        var result = await _budgetService.UpdateLimitAsync(id, limit, ct);
        return FromResult(result);
    }
}

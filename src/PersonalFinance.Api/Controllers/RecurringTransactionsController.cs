using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class RecurringTransactionsController : BaseController
{
    private readonly IRecurringTransactionService _recurringService;

    public RecurringTransactionsController(ICurrentUser currentUser, IRecurringTransactionService recurringService)
        : base(currentUser)
    {
        _recurringService = recurringService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecurringTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _recurringService.GetAsync(ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RecurringTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRecurringTransactionRequest request, CancellationToken ct)
    {
        var result = await _recurringService.CreateAsync(request, ct);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _recurringService.DeactivateAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost("execute-due")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecuteDue(CancellationToken ct)
    {
        var result = await _recurringService.ExecuteDueAsync(ct);
        return FromResult(result);
    }
}

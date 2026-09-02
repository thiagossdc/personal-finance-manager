using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Api.Controllers;

public sealed class TransactionsController : BaseController
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ICurrentUser currentUser, ITransactionService transactionService)
        : base(currentUser)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] TransactionType? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _transactionService.GetTransactionsAsync(accountId, categoryId, type, from, to, page, pageSize, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _transactionService.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var result = await _transactionService.CreateAsync(request, ct);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken ct)
    {
        var result = await _transactionService.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _transactionService.DeleteAsync(id, ct);
        return FromResult(result);
    }
}

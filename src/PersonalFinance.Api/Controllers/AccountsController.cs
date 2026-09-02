using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class AccountsController : BaseController
{
    private readonly IAccountService _accountService;

    public AccountsController(ICurrentUser currentUser, IAccountService accountService)
        : base(currentUser)
    {
        _accountService = accountService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<AccountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _accountService.GetAccountsAsync(page, pageSize, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _accountService.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _accountService.CreateAsync(request, ct);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken ct)
    {
        var result = await _accountService.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _accountService.DeleteAsync(id, ct);
        return FromResult(result);
    }
}

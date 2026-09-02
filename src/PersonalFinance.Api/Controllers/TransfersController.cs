using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class TransfersController : BaseController
{
    private readonly ITransferService _transferService;

    public TransfersController(ICurrentUser currentUser, ITransferService transferService)
        : base(currentUser)
    {
        _transferService = transferService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<TransferDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _transferService.GetAsync(page, pageSize, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest request, CancellationToken ct)
    {
        var result = await _transferService.CreateAsync(request, ct);
        return FromResult(result);
    }
}

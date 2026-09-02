using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class CreditCardsController : BaseController
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardsController(ICurrentUser currentUser, ICreditCardService creditCardService)
        : base(currentUser)
    {
        _creditCardService = creditCardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CreditCardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCards(CancellationToken ct)
    {
        var result = await _creditCardService.GetCardsAsync(ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CreditCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _creditCardService.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreditCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request, CancellationToken ct)
    {
        var result = await _creditCardService.CreateCardAsync(request, ct);
        return FromResult(result);
    }

    [HttpPost("purchases")]
    [ProducesResponseType(typeof(ApiResponse<CreditCardPurchaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreateCreditCardPurchaseRequest request, CancellationToken ct)
    {
        var result = await _creditCardService.CreatePurchaseAsync(request, ct);
        return FromResult(result);
    }

    [HttpPost("purchases/{purchaseId:guid}/pay")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkPurchasePaid(Guid purchaseId, CancellationToken ct)
    {
        var result = await _creditCardService.MarkPurchasePaidAsync(purchaseId, ct);
        return FromResult(result);
    }
}

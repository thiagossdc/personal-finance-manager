using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class CategoriesController : BaseController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICurrentUser currentUser, ICategoryService categoryService)
        : base(currentUser)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _categoryService.GetCategoriesAsync(ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(request, ct);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _categoryService.DeactivateAsync(id, ct);
        return FromResult(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(ICurrentUser currentUser, IDashboardService dashboardService)
        : base(currentUser)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard([FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
    {
        var result = await _dashboardService.GetDashboardAsync(month, year, ct);
        return FromResult(result);
    }
}

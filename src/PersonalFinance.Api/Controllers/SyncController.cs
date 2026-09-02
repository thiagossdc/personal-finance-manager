using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Api.Controllers;

public sealed class SyncController : BaseController
{
    private readonly ISyncService _syncService;

    public SyncController(ICurrentUser currentUser, ISyncService syncService)
        : base(currentUser)
    {
        _syncService = syncService;
    }

    [HttpPost("push")]
    [ProducesResponseType(typeof(ApiResponse<SyncPushResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Push([FromBody] SyncPushRequest request, CancellationToken ct)
    {
        var result = await _syncService.PushAsync(request, ct);
        return FromResult(result);
    }

    [HttpPost("pull")]
    [ProducesResponseType(typeof(ApiResponse<SyncPullResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Pull([FromBody] SyncPullRequest request, CancellationToken ct)
    {
        var result = await _syncService.PullAsync(request, ct);
        return FromResult(result);
    }
}

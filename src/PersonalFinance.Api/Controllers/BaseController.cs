using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Common;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    protected BaseController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected Guid UserId => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("User is not authenticated.");

    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<T> { Success = true, Data = result.Value });
        }

        var error = result.Error!;
        return ToErrorResponse<T>(error);
    }

    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<object> { Success = true });
        }

        var error = result.Error!;
        return ToErrorResponse<object>(error);
    }

    private ObjectResult ToErrorResponse<T>(Domain.Common.Error error)
    {
        var statusCode = HttpStatusMapping.Map(error.Code);
        var response = new ApiResponse<T> { Success = false, Errors = [new ApiError(error.Code, error.Message)] };
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => BadRequest(response),
            StatusCodes.Status401Unauthorized => Unauthorized(response),
            StatusCodes.Status403Forbidden => StatusCode(StatusCodes.Status403Forbidden, response),
            StatusCodes.Status404NotFound => NotFound(response),
            StatusCodes.Status409Conflict => Conflict(response),
            StatusCodes.Status422UnprocessableEntity => StatusCode(StatusCodes.Status422UnprocessableEntity, response),
            _ => StatusCode(StatusCodes.Status500InternalServerError, response)
        };
    }
}

using System.Net;
using System.Text.Json;
using PersonalFinance.Api.Common;
using PersonalFinance.Domain.Common;

namespace PersonalFinance.Api.Middleware;

/// <summary>
/// Captura exceções não tratadas e converte em resposta padronizada.
/// Nunca expõe stack trace ou detalhes internos em produção.
/// </summary>
public static partial class ExceptionHandlingLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    public static partial void UnhandledException(this ILogger logger, Exception ex, string method, string path);
}

/// <summary>
/// Serialização consistente com as opções de JSON da API (camelCase), usada pelo middleware.
/// </summary>
public static class ApiResponseJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.UnhandledException(ex, context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errors) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new[] { new ApiError("UNAUTHORIZED", "Authentication is required.") }),
            KeyNotFoundException => (StatusCodes.Status404NotFound, new[] { new ApiError("NOT_FOUND", "Resource not found.") }),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, new[] { new ApiError("VALIDATION_ERROR", argEx.Message) }),
            InvalidOperationException invEx => (StatusCodes.Status422UnprocessableEntity, new[] { new ApiError("BUSINESS_RULE", invEx.Message) }),
            _ => (StatusCodes.Status500InternalServerError, new[] { new ApiError("INTERNAL_ERROR", "An unexpected error occurred.") })
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiResponse<object>
        {
            Success = false,
            Data = null,
            Errors = errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, ApiResponseJson.Options));
    }
}

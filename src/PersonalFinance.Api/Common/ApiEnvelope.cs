using System.Text.Json.Serialization;
using PersonalFinance.Domain.Common;

namespace PersonalFinance.Api.Common;

/// <summary>
/// Envelope padronizado de resposta da API.
/// <c>{ "success": true, "data": {...}, "errors": [] }</c>
/// </summary>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<ApiError> Errors { get; init; } = Array.Empty<ApiError>();
}

public sealed record ApiError(string Code, string Message);

/// <summary>Mapeamento entre os códigos de erro do domínio e os status HTTP da API.</summary>
public static class HttpStatusMapping
{
    public static int Map(string code) => code switch
    {
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
        "CONFLICT" => StatusCodes.Status409Conflict,
        "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        "BUSINESS_RULE" => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    public static string Description(string code) => code switch
    {
        "NOT_FOUND" => "The requested resource was not found.",
        "VALIDATION_ERROR" => "The request failed validation.",
        "CONFLICT" => "The request conflicts with the current state.",
        "UNAUTHORIZED" => "Authentication is required or credentials are invalid.",
        "FORBIDDEN" => "Access to the requested resource is denied.",
        "BUSINESS_RULE" => "The request violates a business rule.",
        _ => "An unexpected error occurred."
    };
}

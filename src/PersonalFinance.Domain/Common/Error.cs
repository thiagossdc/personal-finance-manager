namespace PersonalFinance.Domain.Common;

/// <summary>
/// Código de erro padronizado usado no Result pattern e nas respostas da API.
/// </summary>
public record Error(string Code, string Message)
{
    public static Error Validation(string message) => new("VALIDATION_ERROR", message);

    public static Error NotFound(string message) => new("NOT_FOUND", message);

    public static Error Conflict(string message) => new("CONFLICT", message);

    public static Error Business(string message) => new("BUSINESS_RULE", message);

    public static Error Unauthorized(string message = "Authentication required.") => new("UNAUTHORIZED", message);

    public static Error Forbidden(string message = "Access denied.") => new("FORBIDDEN", message);

    public static Error Unexpected(string message = "An unexpected error occurred.") => new("UNEXPECTED", message);
}

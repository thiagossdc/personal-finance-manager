using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PersonalFinance.Api.Common;

namespace PersonalFinance.Api.Filters;

/// <summary>
/// Executa automaticamente os <see cref="IValidator{T}"/> registrados (FluentValidation)
/// sobre os argumentos de ação recebidos no corpo da requisição, curto-circuitando com
/// 400 e o envelope padrão de erro caso haja falhas. Mantém as validações de negócio
/// nos services como segunda camada de defesa.
/// </summary>
public sealed class FluentValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var errors = new List<ApiError>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validators = context.HttpContext.RequestServices.GetServices(validatorType).OfType<IValidator>();

            foreach (var validator in validators)
            {
                var result = ((IValidator)validator).Validate(new ValidationContext<object>(argument));
                errors.AddRange(result.Errors.Select(f =>
                    new ApiError("VALIDATION_ERROR", $"{f.PropertyName}: {f.ErrorMessage}")));
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(
                new ApiResponse<object> { Success = false, Errors = errors });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Nenhuma ação necessária após a execução.
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Racinglazing.Forum.Api.Common;

/// <summary>
/// Runs the registered FluentValidation validator (if any) for each action
/// argument and short-circuits with the contract's { error } envelope on
/// failure. Registered globally so controllers don't repeat validation wiring.
/// </summary>
public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (result.IsValid) continue;

            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage).Distinct());
            context.Result = new BadRequestObjectResult(
                new ErrorEnvelope(new ErrorBody("VALIDATION_ERROR", message)));
            return;
        }

        await next();
    }
}

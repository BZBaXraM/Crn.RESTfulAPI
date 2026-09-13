using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters;

/// <summary>
/// Runs the registered FluentValidation validator (if any) for every action argument
/// and short-circuits with a 400 ValidationProblemDetails on failure.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (result.IsValid)
            {
                continue;
            }

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            });
            return;
        }

        await next();
    }
}

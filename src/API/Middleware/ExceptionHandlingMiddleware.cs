using System.Diagnostics;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Domain.Exceptions.ValidationException;

namespace API.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 ProblemDetails responses with a consistent shape.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request {Method} {Path} was cancelled by the client.", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Unhandled exception after the response had started for {Method} {Path}.", context.Request.Method, context.Request.Path);
            throw exception;
        }

        var problem = MapToProblemDetails(exception);
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request {Method} {Path} failed with {StatusCode}.", context.Request.Method, context.Request.Path, problem.Status);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), context.RequestAborted);
    }

    private ProblemDetails MapToProblemDetails(Exception exception) => exception switch
    {
        NotFoundException e => new ProblemDetails
        {
            Title = "Resource not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = e.Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
        },
        ValidationException e => new ValidationProblemDetails(e.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        },
        FluentValidation.ValidationException e => new ValidationProblemDetails(
            e.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()))
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        },
        AuthenticationFailedException e => new ProblemDetails
        {
            Title = "Authentication failed.",
            Status = StatusCodes.Status401Unauthorized,
            Detail = e.Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
        },
        ConflictException e => new ProblemDetails
        {
            Title = "Conflict.",
            Status = StatusCodes.Status409Conflict,
            Detail = e.Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
        },
        DomainException e => new ProblemDetails
        {
            Title = "Business rule violated.",
            Status = StatusCodes.Status400BadRequest,
            Detail = e.Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        },
        UnauthorizedAccessException => new ProblemDetails
        {
            Title = "Unauthorized.",
            Status = StatusCodes.Status401Unauthorized,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
        },
        _ => new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = _environment.IsDevelopment() ? exception.ToString() : "An internal server error occurred. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        }
    };
}

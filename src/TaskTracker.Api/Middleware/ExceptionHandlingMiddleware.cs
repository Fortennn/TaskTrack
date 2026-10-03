using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found"),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Resource conflict"),
            ValidationException => (
                StatusCodes.Status422UnprocessableEntity,
                "Business rule violation"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Request failed with {Status}: {Message}",
                status,
                exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "The server could not complete the request."
                : exception.Message,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = Activity.Current?.Id
            ?? context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: context.RequestAborted);
    }
}

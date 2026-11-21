using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Api.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // A client disconnect is not a server error, and nobody is left to read a response.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        // Two requests changed the same row, such as a double delete. The client can retry.
        if (exception is DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "Concurrent update rejected for {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            return await WriteProblemAsync(
                httpContext,
                exception,
                StatusCodes.Status409Conflict,
                "This item was changed or deleted by another request. Refresh and try again.",
                detail: null);
        }

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        return await WriteProblemAsync(
            httpContext,
            exception,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            environment.IsDevelopment() ? exception.ToString() : null);
    }

    private ValueTask<bool> WriteProblemAsync(
        HttpContext httpContext, Exception exception, int status, string title, string? detail)
    {
        httpContext.Response.StatusCode = status;

        return problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}

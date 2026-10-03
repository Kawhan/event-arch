using EventArch.Application.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventArch.Api.Errors;

/// <summary>
/// Last line of defense for exceptions. Expected business failures never get here
/// (they travel as <c>Result</c>); this handles concurrency conflicts and real bugs.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        if (exception is ConcurrencyConflictException)
        {
            logger.LogWarning(exception, "Concurrency conflict");
            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Detail = exception.Message,
                Extensions = { ["code"] = "Concurrency.Conflict" }
            };
        }
        else
        {
            logger.LogError(exception, "Unhandled exception");
            // Internal details are logged, never returned to the client.
            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred."
            };
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}

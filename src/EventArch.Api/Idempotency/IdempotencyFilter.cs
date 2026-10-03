using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventArch.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventArch.Api.Idempotency;

/// <summary>
/// Makes an action safe to retry. The first request with a given Idempotency-Key runs
/// normally and its response is stored in the same transaction as the operation;
/// a retry with the same key and body gets that stored response without running again.
/// </summary>
internal sealed class IdempotencyFilter(IIdempotencyStore idempotencyStore) : IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";
    private const int KeyMaxLength = 100;

    // Same settings MVC uses to write responses, so a replay is byte-for-byte equivalent.
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        HttpContext httpContext = context.HttpContext;
        CancellationToken cancellationToken = httpContext.RequestAborted;

        string? key = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key) || key.Length > KeyMaxLength)
        {
            context.Result = Problem(
                StatusCodes.Status400BadRequest,
                "Idempotency.KeyRequired",
                $"The '{HeaderName}' header is required (1 to {KeyMaxLength} characters).");
            return;
        }

        string requestHash = ComputeRequestHash(context);

        IdempotencyRecord? existing = await idempotencyStore.FindAsync(key, cancellationToken);
        if (existing is not null)
        {
            context.Result = existing.RequestHash == requestHash
                ? Replay(existing, httpContext)
                : Problem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Idempotency.KeyReused",
                    $"This '{HeaderName}' was already used for a different request.");
            return;
        }

        await using IIdempotencyTransaction transaction = await idempotencyStore.BeginTransactionAsync(cancellationToken);

        ActionExecutedContext executed = await next();

        // Exceptions (bugs, concurrency conflicts) are not stored: leaving without
        // committing rolls the operation back and lets the client retry with the same key.
        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            return;
        }

        (int statusCode, string? responseBody) = CaptureResponse(executed.Result);

        // Server errors may be temporary, so they are not stored and a retry runs again.
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return;
        }

        await transaction.CommitAsync(new IdempotencyRecord(key, requestHash, statusCode, responseBody), cancellationToken);
    }

    /// <summary>
    /// Fingerprint of what the request asks for: method, path and body.
    /// Detects a key that is reused for a different operation.
    /// </summary>
    private static string ComputeRequestHash(ActionExecutingContext context)
    {
        object? body = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => context.ActionArguments.TryGetValue(parameter.Name, out object? value) ? value : null)
            .FirstOrDefault();

        HttpRequest request = context.HttpContext.Request;
        string fingerprint = $"{request.Method} {request.Path}\n{JsonSerializer.Serialize(body, ResponseJsonOptions)}";

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
    }

    private static (int StatusCode, string? ResponseBody) CaptureResponse(IActionResult? result) => result switch
    {
        ObjectResult objectResult => (
            objectResult.StatusCode ?? StatusCodes.Status200OK,
            JsonSerializer.Serialize(objectResult.Value, ResponseJsonOptions)),
        IStatusCodeActionResult statusCodeResult => (statusCodeResult.StatusCode ?? StatusCodes.Status200OK, null),
        _ => (StatusCodes.Status200OK, null)
    };

    private static IActionResult Replay(IdempotencyRecord record, HttpContext httpContext)
    {
        httpContext.Response.Headers[ReplayedHeaderName] = "true";

        if (record.ResponseBody is null)
        {
            return new StatusCodeResult(record.StatusCode);
        }

        return new ContentResult
        {
            StatusCode = record.StatusCode,
            Content = record.ResponseBody,
            ContentType = "application/json"
        };
    }

    private static ObjectResult Problem(int statusCode, string code, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Detail = detail,
            Extensions = { ["code"] = code }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}

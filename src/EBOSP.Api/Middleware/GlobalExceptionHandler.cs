using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Middleware;

/// <summary>
/// Catches any exception that escapes the pipeline and turns it into a standardized RFC 7807
/// ProblemDetails response instead of leaking a stack trace (dev guide §9: global exception
/// handling and standardized error responses). Never logs the exception message into the
/// response body - only into the server-side log, correlated via <see cref="CorrelationIdMiddleware"/>.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdMiddleware.HeaderName] as string;

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path} (correlation {CorrelationId})",
            httpContext.Request.Method,
            httpContext.Request.Path,
            correlationId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = httpContext.Request.Path,
        };
        if (correlationId is not null)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

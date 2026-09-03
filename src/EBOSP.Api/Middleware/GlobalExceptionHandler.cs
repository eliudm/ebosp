using EBOSP.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Middleware;

/// <summary>
/// Catches any exception that escapes the pipeline and turns it into a standardized RFC 7807
/// ProblemDetails response instead of leaking a stack trace (dev guide §9: global exception
/// handling and standardized error responses). Never logs the exception message into the
/// response body - only into the server-side log, correlated via <see cref="CorrelationIdMiddleware"/>.
/// Known, expected exceptions (<see cref="EBOSP.Application.Common"/>) map to their specific
/// status code and are logged at a lower level; anything else is an unexpected 500.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdMiddleware.HeaderName] as string;
        var (status, title) = Classify(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path} (correlation {CorrelationId})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }
        else
        {
            logger.LogWarning(
                "{Title} for {Method} {Path} (correlation {CorrelationId})",
                title,
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
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

    private static (int Status, string Title) Classify(Exception exception) => exception switch
    {
        AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Authentication failed."),
        MfaChallengeRequiredException => (StatusCodes.Status401Unauthorized, "MFA challenge required."),
        ForbiddenOperationException e => (StatusCodes.Status403Forbidden, e.Message),
        NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
        ConflictException e => (StatusCodes.Status409Conflict, e.Message),
        ConcurrencyConflictException => (StatusCodes.Status409Conflict, "The record was modified concurrently. Please retry."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
    };
}

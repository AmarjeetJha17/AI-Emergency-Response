using EmergencyResponse.Application.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Common.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        _logger.LogError(
            "Unhandled exception category {ExceptionType}. TraceId: {TraceId}",
            exception.GetType().Name,
            traceId);

        var statusCode = StatusCodes.Status500InternalServerError;

        if (exception is UnauthorizedException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
        }
        else if (exception is ConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
        }
        else if (exception is AIFeatureDisabledException)
        {
            statusCode = StatusCodes.Status503ServiceUnavailable;
        }
        else if (HasDatabaseTransportFailure(exception))
        {
            statusCode = StatusCodes.Status503ServiceUnavailable;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = exception is AIFeatureDisabledException
                ? "AI feature is disabled."
                : statusCode == StatusCodes.Status503ServiceUnavailable
                ? "A required service is temporarily unavailable."
                : "An error occurred.",
            Detail = exception is AIFeatureDisabledException
                ? exception.Message
                : statusCode == StatusCodes.Status503ServiceUnavailable
                ? "The request could not be persisted. Please retry after service recovery."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }

    private static bool HasDatabaseTransportFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is System.Data.Common.DbException or TimeoutException)
                return true;
        }
        return false;
    }
}

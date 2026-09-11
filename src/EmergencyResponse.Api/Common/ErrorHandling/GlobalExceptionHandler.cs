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
            exception,
            "Unhandled exception. TraceId: {TraceId}",
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

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "An error occurred.",
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}
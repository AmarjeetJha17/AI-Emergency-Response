using System.Diagnostics;
using System.Text.Json;
using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class CopilotToolInvocationRecorder
{
    private readonly ApplicationDbContext _db;
    private readonly ICopilotExecutionContext _context;

    public CopilotToolInvocationRecorder(
        ApplicationDbContext db,
        ICopilotExecutionContext context)
    {
        _db = db;
        _context = context;
    }

    public async ValueTask<object?> InvokeAsync(
        Microsoft.Extensions.AI.FunctionInvocationContext invocation,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var succeeded = false;
        object? result = null;
        string? failureReason = null;
        try
        {
            result = await invocation.Function.InvokeAsync(
                invocation.Arguments,
                cancellationToken);
            succeeded = true;
            return result;
        }
        catch (Exception ex)
        {
            failureReason = ex.GetType().Name;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            EmergencyResponseTelemetry.CopilotToolCalls.Add(1);
            EmergencyResponseTelemetry.CopilotToolDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds);
            if (!succeeded)
            {
                EmergencyResponseTelemetry.CopilotToolFailures.Add(1);
            }

            _db.CopilotToolInvocations.Add(new CopilotToolInvocation
            {
                Id = Guid.NewGuid(),
                CopilotInteractionId = _context.InteractionId,
                ToolName = invocation.Function.Name,
                ArgumentsJson = JsonSerializer.Serialize(invocation.Arguments),
                ResultSummaryJson = succeeded
                    ? JsonSerializer.Serialize(new
                    {
                        success = true,
                        resultType = result?.GetType().Name
                    })
                    : null,
                Succeeded = succeeded,
                FailureReason = failureReason,
                InvokedAtUtc = DateTime.UtcNow,
                ExecutionTimeMs = (long)stopwatch.Elapsed.TotalMilliseconds
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

using EmergencyResponse.Application.Observability;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmergencyResponse.Api.Health;

public sealed class BackgroundWorkerHealthCheck : IHealthCheck
{
    private readonly IWorkerHealthState _state;

    public BackgroundWorkerHealthCheck(IWorkerHealthState state)
    {
        _state = state;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var lastSuccess = _state.LastSuccessUtc;

        if (lastSuccess is null)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("Worker has not completed successfully yet."));
        }

        if (DateTime.UtcNow - lastSuccess.Value > TimeSpan.FromMinutes(10))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Worker has not completed successfully recently."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Background workers are running normally."));
    }
}

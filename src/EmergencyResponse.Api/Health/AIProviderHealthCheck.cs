using EmergencyResponse.Application.Common.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Api.Health;

public sealed class AIProviderHealthCheck : IHealthCheck
{
    private readonly AIOptions _options;

    public AIProviderHealthCheck(IOptions<AIOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) && string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("AI credentials not configured; deterministic operations active."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("AI provider configured."));
    }
}

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
        if (!_options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "AI is disabled for this deployment; provider checks are not applicable."));
        }

        if (_options.DeploymentAvailabilityVerified == false)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "AI model deployment availability and workload permissions have not been verified."));
        }

        if (_options.DeploymentAvailabilityVerified == true &&
            (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint) ||
             endpoint.Scheme != Uri.UriSchemeHttps ||
             string.IsNullOrWhiteSpace(_options.Deployment) ||
             string.IsNullOrWhiteSpace(_options.EmbeddingDeployment)))
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "AI endpoint or deployment configuration is incomplete."));
        }

        if (_options.DeploymentAvailabilityVerified == true)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "The operator marked model availability and workload permissions verified; this check does not probe the provider."));
        }

        // Preserve the historical check for environments that do not set the
        // portfolio-demo verification signal.
        if (string.IsNullOrWhiteSpace(_options.Endpoint) && string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("AI credentials not configured; deterministic operations active."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("AI provider configured."));
    }
}

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmergencyResponse.Api.Health;

public sealed class PredictiveModelHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public PredictiveModelHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var configuredPath = _configuration["Predictive:ModelPath"]
            ?? "models/escalation-risk/current.json";
        var pointerPath = ResolvePath(configuredPath);

        if (configuredPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(pointerPath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(pointerPath));
                var activeVersion = document.RootElement
                    .GetProperty("activeVersion")
                    .GetString();
                if (!string.IsNullOrWhiteSpace(activeVersion))
                {
                    var modelPath = Path.Combine(
                        Path.GetDirectoryName(pointerPath)!,
                        activeVersion,
                        "model.zip");
                    var manifestPath = Path.Combine(
                        Path.GetDirectoryName(pointerPath)!,
                        activeVersion,
                        "manifest.json");
                    return File.Exists(modelPath) && File.Exists(manifestPath)
                        ? Task.FromResult(HealthCheckResult.Healthy(
                            $"Predictive model {activeVersion} is available."))
                        : Task.FromResult(HealthCheckResult.Degraded(
                            $"Predictive model {activeVersion} or manifest is unavailable."));
                }
            }
            catch (JsonException)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Predictive model pointer is invalid."));
            }
        }

        return Task.FromResult(File.Exists(pointerPath)
            ? HealthCheckResult.Healthy("Predictive model is available.")
            : HealthCheckResult.Degraded(
                $"Predictive model is unavailable at '{pointerPath}'."));
    }

    private static string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, configuredPath),
            Path.Combine(Directory.GetCurrentDirectory(), configuredPath),
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "src",
                "EmergencyResponse.Api",
                configuredPath)
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}

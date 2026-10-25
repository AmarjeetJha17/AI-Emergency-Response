namespace EmergencyResponse.Application.Common.Configuration;

public sealed class AIOptions
{
    public const string SectionName = "AI";

    /// <summary>Controls whether provider-backed AI services are registered and available.</summary>
    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = null!;

    public string Endpoint { get; set; } = null!;

    public string? ApiKey { get; set; }

    public string Deployment { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string EmbeddingDeployment { get; set; } = null!;

    /// <summary>
    /// Operator verification that configured model deployments exist, the
    /// workload identity is authorized, and a synthetic smoke request passed.
    /// This is an explicit status signal, not a live provider probe.
    /// </summary>
    public bool? DeploymentAvailabilityVerified { get; set; }
}

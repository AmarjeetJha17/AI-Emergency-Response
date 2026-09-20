namespace EmergencyResponse.Application.Common.Configuration;

public sealed class AIOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = null!;

    public string Endpoint { get; set; } = null!;

    public string? ApiKey { get; set; }

    public string Deployment { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string EmbeddingDeployment { get; set; } = null!;
}
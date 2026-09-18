namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record ReviewVisualAnalysisRequest
{
    public bool Accepted { get; init; }

    public string? Decision { get; init; }
}

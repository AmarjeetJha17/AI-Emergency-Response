namespace EmergencyResponse.Application.AI.DTOs;

public sealed record AIAnalysisRequest
{
    public Guid IncidentId { get; init; }

    public IncidentAnalysisInput Input { get; init; } = null!;
}
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record IncidentMediaResponse
{
    public Guid Id { get; init; }

    public Guid IncidentId { get; init; }

    public string ContentType { get; init; } = null!;

    public long SizeBytes { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public bool AIAnalysisRequested { get; init; }

    public IncidentMediaType MediaType { get; init; }

    public VisualAnalysisStatus? VisualAnalysisStatus { get; init; }

    public double? VisualAnalysisConfidence { get; init; }

    public string? MediaUrl { get; init; }

    public VisualAnalysisResponse? Analysis { get; init; }
}

using EmergencyResponse.Application.DTOs.Incidents;

namespace EmergencyResponse.Web.Models.Incidents;

public sealed class VisualEvidenceViewModel
{
    public Guid IncidentId { get; init; }

    public Guid MediaId { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string MediaUrl { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public VisualAnalysisResponse? Analysis { get; init; }
}

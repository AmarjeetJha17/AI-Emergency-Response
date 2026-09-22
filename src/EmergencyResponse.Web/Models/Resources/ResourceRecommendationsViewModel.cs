using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Resources;

namespace EmergencyResponse.Web.Models.Resources;

public sealed class ResourceRecommendationsViewModel
{
    public IncidentResponse Incident { get; init; } = null!;

    public ResourceRecommendationResponse Recommendation { get; init; } = null!;
}

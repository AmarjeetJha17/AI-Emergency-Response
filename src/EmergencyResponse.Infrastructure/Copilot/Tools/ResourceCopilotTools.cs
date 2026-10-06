using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Resources;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class ResourceCopilotTools
{
    private readonly IResourceService _service;
    private readonly CopilotToolAuthorization _authorization;

    public ResourceCopilotTools(
        IResourceService service,
        CopilotToolAuthorization authorization)
    {
        _service = service;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<CopilotResourceSummary>> GetResourceSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        _authorization.RequireAuthenticated();
        var summary = await _service.GetSummaryAsync(cancellationToken);
        return CopilotToolResult<CopilotResourceSummary>.Available(
            new CopilotResourceSummary
            {
                TotalResources = summary.Available + summary.Assigned +
                                 summary.Dispatched + summary.EnRoute +
                                 summary.OnScene + summary.Returning +
                                 summary.Unavailable + summary.Maintenance,
                Available = summary.Available,
                Assigned = summary.Assigned,
                Dispatched = summary.Dispatched,
                EnRoute = summary.EnRoute,
                OnScene = summary.OnScene,
                Unavailable = summary.Unavailable +
                             summary.Maintenance
            });
    }

    public async Task<CopilotToolResult<IReadOnlyList<CopilotResourceRecommendation>>>
        GetResourceRecommendationsAsync(
            Guid incidentId,
            CancellationToken cancellationToken = default)
    {
        CopilotToolValidation.RequiredId(incidentId, nameof(incidentId));
        await _authorization.RequireIncidentAccessAsync(
            incidentId,
            cancellationToken);
        var response = await _service.GetRecommendationsAsync(
            incidentId,
            cancellationToken);
        return CopilotToolResult<IReadOnlyList<CopilotResourceRecommendation>>.Available(
            response.Recommendations
                .Take(20)
                .Select(x => new CopilotResourceRecommendation
                {
                    ResourceId = x.ResourceId,
                    ResourceName = x.ResourceName,
                    ResourceType = x.ResourceType.ToString(),
                    DistanceKm = x.DistanceKm ?? 0,
                    RecommendationScore = x.RecommendationScore,
                    Reasons = x.Reasons
                })
                .ToList());
    }
}

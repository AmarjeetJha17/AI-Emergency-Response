using EmergencyResponse.Application.Resources;

namespace EmergencyResponse.Web.Models.Resources;

public sealed class ResourceCommandCenterViewModel
{
    public ResourceSummary Summary { get; init; } = new();
    public IReadOnlyList<ResourceCommandCenterItem> Resources { get; init; } = [];
}

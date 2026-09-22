using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceRequirementDto
{
    public ResourceType ResourceType { get; init; }

    public int MinimumCount { get; init; }

    public int PreferredCount { get; init; }

    public bool IsMandatory { get; init; }

    public int PriorityOrder { get; init; }
}

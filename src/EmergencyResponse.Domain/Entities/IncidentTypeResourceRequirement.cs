using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class IncidentTypeResourceRequirement : BaseEntity
{
    public Guid IncidentTypeId { get; set; }

    public IncidentType IncidentType { get; set; } = null!;

    public ResourceType ResourceType { get; set; }

    public int MinimumCount { get; set; } = 1;

    public int PreferredCount { get; set; } = 1;

    public bool IsMandatory { get; set; } = true;

    public int PriorityOrder { get; set; } = 1;
}

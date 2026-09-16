using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class IncidentRelation : BaseEntity
{
    public Guid ParentIncidentId { get; set; }

    public Incident ParentIncident { get; set; } = null!;

    public Guid RelatedIncidentId { get; set; }

    public Incident RelatedIncident { get; set; } = null!;

    public IncidentRelationType RelationType { get; set; }

    public string? Notes { get; set; }

    public string? CreatedByUserId { get; set; }
}
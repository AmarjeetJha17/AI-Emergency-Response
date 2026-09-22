using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class ResourceStatusHistory : BaseEntity
{
    public Guid ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    public ResourceStatus PreviousStatus { get; set; }

    public ResourceStatus NewStatus { get; set; }

    public Guid? IncidentId { get; set; }

    public Incident? Incident { get; set; }

    public string? ChangedByUserId { get; set; }

    public string? Reason { get; set; }

    public DateTime ChangedAtUtc { get; set; }
}

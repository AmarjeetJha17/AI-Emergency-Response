using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class ResourceAssignment : BaseEntity
{
    public Guid ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? ReleasedAtUtc { get; set; }

    public string? Remarks { get; set; }
}
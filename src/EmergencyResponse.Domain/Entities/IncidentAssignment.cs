using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class IncidentAssignment : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public Guid DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public Guid? ResponderUserId { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? Remarks { get; set; }
}
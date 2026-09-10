using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class IncidentStatusHistory : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public IncidentStatus OldStatus { get; set; }

    public IncidentStatus NewStatus { get; set; }

    public string? ChangedByUserId { get; set; }

    public string? Remarks { get; set; }

    public DateTime ChangedAtUtc { get; set; }
}
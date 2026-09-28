using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class OperationalSituationIncident : BaseEntity
{
    public Guid OperationalSituationId { get; set; }

    public OperationalSituation OperationalSituation { get; set; } = null!;

    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public bool IsPrimaryIncident { get; set; }

    public DateTime AddedAtUtc { get; set; }
}

using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class IncidentTag : BaseEntity
{
    public Guid IncidentId { get; set; }

    public string Tag { get; set; } = string.Empty;

    public string AddedByUserId { get; set; } = string.Empty;
}

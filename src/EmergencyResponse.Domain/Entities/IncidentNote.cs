using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class IncidentNote : BaseEntity
{
    public Guid IncidentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;
}

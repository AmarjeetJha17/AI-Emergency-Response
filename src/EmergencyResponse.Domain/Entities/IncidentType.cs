using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class IncidentType : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Incident> Incidents { get; set; }
        = new List<Incident>();
}
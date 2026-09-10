using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Incident> Incidents { get; set; }
        = new List<Incident>();

    public ICollection<Resource> Resources { get; set; }
        = new List<Resource>();
}
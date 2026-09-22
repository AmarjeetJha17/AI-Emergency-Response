using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class Resource : BaseEntity
{
    public string Name { get; set; } = null!;

    public ResourceType Type { get; set; }

    public ResourceStatus Status { get; set; }

    public Guid DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? LocationUpdatedAtUtc { get; set; }

    public bool ShowOnOperationalMap { get; set; } = true;

    public int? Capacity { get; set; }

    public ICollection<ResourceAssignment> Assignments { get; set; }
        = new List<ResourceAssignment>();
}
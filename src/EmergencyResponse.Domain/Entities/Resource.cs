using System.Security.AccessControl;

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

    public int? Capacity { get; set; }

    public ICollection<ResourceAssignment> Assignments { get; set; }
        = new List<ResourceAssignment>();
}

public enum ResourceType
{
    Ambulance = 1,
    FireTruck = 2,
    RescueVehicle = 3,
    PoliceVehicle = 4,
    RescueTeam = 5,
    MedicalTeam = 6,
    Boat = 7,
    Excavator = 8,
    Shelter = 9,
    Other = 10
}

public enum ResourceStatus
{
    Available = 1,
    Assigned = 2,
    Busy = 3,
    Offline = 4,
    Maintenance = 5
}
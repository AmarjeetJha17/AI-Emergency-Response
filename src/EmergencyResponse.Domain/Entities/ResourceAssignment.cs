using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class ResourceAssignment : BaseEntity
{
    public Guid ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? DispatchedAtUtc { get; set; }

    public DateTime? EnRouteAtUtc { get; set; }

    public DateTime? OnSceneAtUtc { get; set; }

    public DateTime? ReturningAtUtc { get; set; }

    public DateTime? ReleasedAtUtc { get; set; }

    public string? AssignedByUserId { get; set; }

    public string? ReleaseNotes { get; set; }

    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;

    public ResourceAssignmentStatus Status { get; set; }
        = ResourceAssignmentStatus.Active;
}
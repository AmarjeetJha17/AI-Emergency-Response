using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class Incident : BaseEntity
{
    public string IncidentNumber { get; set; } = null!;

    public string? ReporterUserId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public Guid IncidentTypeId { get; set; }

    public IncidentType IncidentType { get; set; } = null!;

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public IncidentStatus Status { get; set; }

    public SeverityLevel Severity { get; set; }

    public PriorityLevel Priority { get; set; }

    public int? PeopleAffected { get; set; }

    public int? VehiclesAffected { get; set; }

    public int? Casualties { get; set; }

    public DateTime ReportedAtUtc { get; set; }

    public ICollection<IncidentLocation> Locations { get; set; }
        = new List<IncidentLocation>();

    public ICollection<IncidentMedia> Media { get; set; }
        = new List<IncidentMedia>();

    public ICollection<IncidentAssignment> Assignments { get; set; }
        = new List<IncidentAssignment>();

    public ICollection<IncidentStatusHistory> StatusHistory { get; set; }
        = new List<IncidentStatusHistory>();

    public ICollection<AIAnalysis> AIAnalyses { get; set; }
        = new List<AIAnalysis>();
}
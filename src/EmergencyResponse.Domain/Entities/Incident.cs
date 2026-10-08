using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

using Microsoft.Data.SqlTypes;

namespace EmergencyResponse.Domain.Entities;

public class Incident : BaseEntity
{
    public DataClassification DataClassification { get; set; } = DataClassification.Confidential;
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

    public int SeverityScore { get; set; }

    public int PriorityScore { get; set; }

    public string? SeverityReasonsJson { get; set; }

    public string? PriorityReasonsJson { get; set; }

    public SqlVector<float>? Embedding { get; set; }

    public string? EmbeddingModel { get; set; }

    public DateTime? EmbeddingGeneratedAtUtc { get; set; }

    public DuplicateDetectionStatus DuplicateDetectionStatus { get; set; }
        = DuplicateDetectionStatus.NotStarted;

    public DateTime? DuplicateDetectionCompletedAtUtc { get; set; }

    public string? DuplicateDetectionFailureReason { get; set; }

    public bool IsCanonical { get; set; } = true;

    public Guid? CanonicalIncidentId { get; set; }

    public Incident? CanonicalIncident { get; set; }
}

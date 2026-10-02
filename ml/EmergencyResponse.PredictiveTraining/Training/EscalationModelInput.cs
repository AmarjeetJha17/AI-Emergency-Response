using Microsoft.ML.Data;

namespace EmergencyResponse.PredictiveTraining.Training;

public sealed class EscalationModelInput
{
    public DateTime SnapshotAtUtc { get; set; }

    public Guid IncidentId { get; set; }

    public DateTime? OutcomeAtUtc { get; set; }

    public string FeatureHash { get; set; } = string.Empty;

    public float SeverityScore { get; set; }

    public float PriorityScore { get; set; }

    public float PeopleAffected { get; set; }

    public float VehiclesAffected { get; set; }

    public float Casualties { get; set; }

    public float ImmediateDanger { get; set; }

    public float PeopleTrapped { get; set; }

    public float CriticalInfrastructureAffected { get; set; }

    public float InfrastructureDamage { get; set; }

    public float RoadBlocked { get; set; }

    public float EscalationCount { get; set; }

    public float DuplicateCount { get; set; }

    public float RelatedIncidentCount { get; set; }

    public float ResourceShortage { get; set; }

    public float IncidentAgeMinutes { get; set; }

    public bool Label { get; set; }
}

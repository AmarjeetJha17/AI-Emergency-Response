using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Application.Predictive;
using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Copilot.Tools;

public sealed class CopilotIncidentResult
{
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IncidentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public int? PeopleAffected { get; init; }
    public int? VehiclesAffected { get; init; }
    public int? Casualties { get; init; }
    public DateTime LastUpdatedAtUtc { get; init; }
}

public sealed class CopilotSituationResult
{
    public Guid SituationId { get; init; }
    public string SituationNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double? SituationScore { get; init; }
    public int IncidentCount { get; init; }
    public int CriticalIncidentCount { get; init; }
    public int HighIncidentCount { get; init; }
    public int PeopleAffected { get; init; }
    public int Casualties { get; init; }
    public bool ResourceShortageDetected { get; init; }
    public DateTime RetrievedAtUtc { get; init; }
    public IReadOnlyList<CopilotSituationIncident> Incidents { get; init; } = [];
}

public sealed class CopilotSituationIncident
{
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public bool IsPrimaryIncident { get; init; }
}

public sealed class CopilotEscalationResult
{
    public Guid Id { get; init; }
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public double Score { get; init; }
    public bool RequiresHumanReview { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CopilotResourceSummary
{
    public int TotalResources { get; init; }
    public int Available { get; init; }
    public int Assigned { get; init; }
    public int Dispatched { get; init; }
    public int EnRoute { get; init; }
    public int OnScene { get; init; }
    public int Unavailable { get; init; }
}

public sealed class CopilotResourceRecommendation
{
    public Guid ResourceId { get; init; }
    public string ResourceName { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public double DistanceKm { get; init; }
    public double RecommendationScore { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}

public sealed class CopilotEnvironmentResult
{
    public DateTime ObservedAtUtc { get; init; }
    public string Provider { get; init; } = string.Empty;
    public double? TemperatureC { get; init; }
    public double? WindSpeedKph { get; init; }
    public double? PrecipitationMm { get; init; }
    public double? VisibilityKm { get; init; }
    public bool HasSevereWeatherAlert { get; init; }
    public IReadOnlyList<string> SevereWeatherTypes { get; init; } = [];
    public double? AirQualityIndex { get; init; }
    public bool SignificantTrafficDisruption { get; init; }
}

public sealed class CopilotPredictionResult
{
    public Guid IncidentId { get; init; }
    public bool PredictedEscalation { get; init; }
    public double Probability { get; init; }
    public double RiskScore { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string ModelVersion { get; init; } = string.Empty;
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime? ValidUntilUtc { get; init; }
    public bool AdvisoryOnly { get; init; } = true;
}

public sealed class CopilotNotificationResult
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Guid? IncidentId { get; init; }
    public string? IncidentNumber { get; init; }
    public Guid? SituationId { get; init; }
    public string? SituationNumber { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

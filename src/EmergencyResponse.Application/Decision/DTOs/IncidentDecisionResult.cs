using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Decision.DTOs;

public sealed record IncidentDecisionResult
{
    public SeverityEvaluationResult Severity { get; init; }
        = null!;

    public PriorityEvaluationResult Priority { get; init; }
        = null!;
}
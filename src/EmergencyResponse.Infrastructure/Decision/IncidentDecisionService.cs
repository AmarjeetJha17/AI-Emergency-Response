using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.AI;

namespace EmergencyResponse.Infrastructure.Decision;

public sealed class IncidentDecisionService
    : IIncidentDecisionService
{
    private readonly ISeverityEngine _severityEngine;
    private readonly IPriorityEngine _priorityEngine;

    public IncidentDecisionService(
        ISeverityEngine severityEngine,
        IPriorityEngine priorityEngine)
    {
        _severityEngine = severityEngine;
        _priorityEngine = priorityEngine;
    }

    public IncidentDecisionResult Evaluate(
        IncidentAnalysisResult analysis)
    {
        var severityInput =
            new SeverityEvaluationInput
            {
                IncidentType = analysis.IncidentType,

                PeopleAffected = analysis.PeopleAffected,

                VehiclesAffected = analysis.VehiclesAffected,

                Casualties = analysis.Casualties,

                PeopleTrapped = analysis.Facts.PeopleTrapped,

                InfrastructureDamage =
                    analysis.Facts.InfrastructureDamage,

                RoadBlocked =
                    analysis.Facts.RoadBlocked,

                ResidentialAreaAffected =
                    analysis.Facts.ResidentialAreaAffected,

                ImmediateDanger =
                    analysis.Facts.ImmediateDanger,

                AICandidateSeverity =
                    Enum.TryParse<SeverityLevel>(
                        analysis.SeverityCandidate,
                        true,
                        out var aiSeverity)
                        ? aiSeverity
                        : null,

                AIConfidence =
                    analysis.Confidence
            };

        var severityResult =
            _severityEngine.Evaluate(
                severityInput);

        var priorityInput =
            new PriorityEvaluationInput
            {
                Severity = severityResult.Severity,

                PeopleAffected =
                    analysis.PeopleAffected,

                Casualties =
                    analysis.Casualties,

                PeopleTrapped =
                    analysis.Facts.PeopleTrapped,

                RoadBlocked =
                    analysis.Facts.RoadBlocked,

                CriticalInfrastructureAffected =
                    analysis.Facts.CriticalInfrastructureAffected,

                EscalationRisk =
                    analysis.Facts.EscalationRisk,

                EmergencyServicesRequired =
                    analysis.Facts.ImmediateDanger
            };

        var priorityResult =
            _priorityEngine.Evaluate(
                priorityInput);

        return new IncidentDecisionResult
        {
            Severity = severityResult,
            Priority = priorityResult
        };
    }
}
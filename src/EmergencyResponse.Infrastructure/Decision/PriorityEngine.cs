using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Infrastructure.Decision;

public sealed class PriorityEngine : IPriorityEngine
{
    public PriorityEvaluationResult Evaluate(
        PriorityEvaluationInput input)
    {
        var score = input.Severity switch
        {
            SeverityLevel.Critical => 60,
            SeverityLevel.High => 40,
            SeverityLevel.Moderate => 20,
            _ => 5
        };

        var reasons = new List<string>
        {
            $"Base severity contribution: {input.Severity}"
        };

        if (input.PeopleTrapped)
        {
            score += 20;
            reasons.Add("People are trapped.");
        }

        if (input.Casualties is > 0)
        {
            score += 20;
            reasons.Add("Casualties reported.");
        }

        if (input.PeopleAffected is >= 20)
        {
            score += 15;
            reasons.Add("Large number of affected people.");
        }

        if (input.RoadBlocked)
        {
            score += 10;
            reasons.Add("Road is blocked.");
        }

        if (input.CriticalInfrastructureAffected)
        {
            score += 15;
            reasons.Add("Critical infrastructure affected.");
        }

        if (input.EscalationRisk)
        {
            score += 15;
            reasons.Add("Escalation risk detected.");
        }

        if (input.EmergencyServicesRequired)
        {
            score += 10;
            reasons.Add("Emergency services required.");
        }

        score = Math.Min(score, 100);

        var priority = score switch
        {
            >= 75 => PriorityLevel.Critical,
            >= 50 => PriorityLevel.High,
            >= 25 => PriorityLevel.Normal,
            _ => PriorityLevel.Low
        };

        return new PriorityEvaluationResult
        {
            Score = score,
            Priority = priority,
            Reasons = reasons
        };
    }
}
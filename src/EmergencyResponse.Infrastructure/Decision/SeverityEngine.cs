using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Infrastructure.Decision;

public sealed class SeverityEngine : ISeverityEngine
{
    public SeverityEvaluationResult Evaluate(
        SeverityEvaluationInput input)
    {
        var score = 0;

        var reasons = new List<string>();

        if (input.Casualties is > 0)
        {
            score += 50;

            reasons.Add(
                $"Casualties reported: {input.Casualties}");
        }

        if (input.PeopleTrapped)
        {
            score += 30;
            reasons.Add("People reported trapped.");
        }

        if (input.ImmediateDanger)
        {
            score += 25;
            reasons.Add("Immediate danger reported.");
        }

        if (input.PeopleAffected is >= 10)
        {
            score += 20;
            reasons.Add(
                $"10 or more people affected: {input.PeopleAffected}");
        }
        else if (input.PeopleAffected is >= 5)
        {
            score += 10;
            reasons.Add(
                $"5 or more people affected: {input.PeopleAffected}");
        }

        if (input.VehiclesAffected is >= 3)
        {
            score += 15;
            reasons.Add(
                $"3 or more vehicles affected: {input.VehiclesAffected}");
        }
        else if (input.VehiclesAffected is >= 1)
        {
            score += 5;
            reasons.Add(
                $"Vehicles affected: {input.VehiclesAffected}");
        }

        if (input.InfrastructureDamage)
        {
            score += 15;
            reasons.Add("Infrastructure damage reported.");
        }

        if (input.RoadBlocked)
        {
            score += 10;
            reasons.Add("Roadway blocked.");
        }

        if (input.ResidentialAreaAffected)
        {
            score += 15;
            reasons.Add("Residential area affected.");
        }

        score = Math.Min(score, 100);

        var severity = score switch
        {
            >= 75 => SeverityLevel.Critical,
            >= 50 => SeverityLevel.High,
            >= 25 => SeverityLevel.Moderate,
            _ => SeverityLevel.Low
        };

        return new SeverityEvaluationResult
        {
            Score = score,
            Severity = severity,
            Reasons = reasons
        };
    }
}
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Escalation;

public static class EscalationEngine
{
    public static IReadOnlyList<EscalationRecommendation> Evaluate(EscalationEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var results = new List<EscalationRecommendation>();

        EvaluateSeverityIncrease(input, results);
        EvaluatePeopleInDanger(input, results);
        EvaluateImmediateDanger(input, results);
        EvaluateCriticalInfrastructure(input, results);
        EvaluateCasualtyIncrease(input, results);
        EvaluateIncidentCluster(input, results);
        EvaluateDuplicateCluster(input, results);
        EvaluateResourceShortage(input, results);

        return results;
    }

    private static void EvaluateSeverityIncrease(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (!input.PreviousSeverity.HasValue)
        {
            return;
        }

        if (input.CurrentSeverity <= input.PreviousSeverity.Value)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.SeverityIncrease,
            Level = input.CurrentSeverity == SeverityLevel.Critical
                ? EscalationLevel.Critical
                : EscalationLevel.High,
            Reason = $"Incident severity increased from {input.PreviousSeverity.Value} to {input.CurrentSeverity}.",
            Evidence =
            [
                $"Previous severity: {input.PreviousSeverity.Value}",
                $"Current severity: {input.CurrentSeverity}"
            ],
            Score = 1.0,
            RequiresHumanReview = true
        });
    }

    private static void EvaluatePeopleInDanger(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (!input.PeopleTrapped)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.PeopleInDanger,
            Level = EscalationLevel.Critical,
            Reason = "People appear to be trapped or unable to reach safety.",
            Evidence =
            [
                "PeopleTrapped = true"
            ],
            Score = 1.0,
            RequiresHumanReview = true
        });
    }

    private static void EvaluateImmediateDanger(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (!input.ImmediateDanger)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.ImmediateDanger,
            Level = EscalationLevel.Critical,
            Reason = "Immediate danger was identified in the incident evidence.",
            Evidence =
            [
                "ImmediateDanger = true"
            ],
            Score = 1.0,
            RequiresHumanReview = true
        });
    }

    private static void EvaluateCriticalInfrastructure(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (!input.CriticalInfrastructureAffected)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.CriticalInfrastructure,
            Level = EscalationLevel.High,
            Reason = "Critical infrastructure appears to be affected.",
            Evidence =
            [
                "CriticalInfrastructureAffected = true"
            ],
            Score = 0.90,
            RequiresHumanReview = true
        });
    }

    private static void EvaluateCasualtyIncrease(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (input.Casualties <= 0)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.CasualtyIncrease,
            Level = input.Casualties >= 5
                ? EscalationLevel.Critical
                : EscalationLevel.High,
            Reason = $"Incident currently reports {input.Casualties} casualties.",
            Evidence =
            [
                $"Casualties: {input.Casualties}"
            ],
            Score = Math.Min(1.0, input.Casualties / 10.0),
            RequiresHumanReview = true
        });
    }

    private static void EvaluateIncidentCluster(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (input.RelatedIncidentCount < 3)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.IncidentCluster,
            Level = input.RelatedIncidentCount >= 10
                ? EscalationLevel.High
                : EscalationLevel.Elevated,
            Reason = $"{input.RelatedIncidentCount} related incidents are associated with this incident.",
            Evidence =
            [
                $"Related incidents: {input.RelatedIncidentCount}"
            ],
            Score = Math.Min(1.0, input.RelatedIncidentCount / 10.0),
            RequiresHumanReview = true
        });
    }

    private static void EvaluateDuplicateCluster(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (input.DuplicateIncidentCount < 3)
        {
            return;
        }

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.DuplicateIncidentCluster,
            Level = input.DuplicateIncidentCount >= 10
                ? EscalationLevel.High
                : EscalationLevel.Elevated,
            Reason = $"{input.DuplicateIncidentCount} duplicate reports have been identified around this incident.",
            Evidence =
            [
                $"Duplicate reports: {input.DuplicateIncidentCount}"
            ],
            Score = Math.Min(1.0, input.DuplicateIncidentCount / 10.0),
            RequiresHumanReview = true
        });
    }

    private static void EvaluateResourceShortage(
        EscalationEvaluationInput input,
        ICollection<EscalationRecommendation> results)
    {
        if (input.RequiredResourceCount <= 0)
        {
            return;
        }

        if (input.AvailableMatchingResourceCount >= input.RequiredResourceCount)
        {
            return;
        }

        var shortage = input.RequiredResourceCount - input.AvailableMatchingResourceCount;

        results.Add(new EscalationRecommendation
        {
            IncidentId = input.IncidentId,
            Type = EscalationType.ResourceShortage,
            Level = shortage >= 3
                ? EscalationLevel.High
                : EscalationLevel.Elevated,
            Reason = $"Required resources exceed the currently available matching resources by {shortage}.",
            Evidence =
            [
                $"Required: {input.RequiredResourceCount}",
                $"Available: {input.AvailableMatchingResourceCount}"
            ],
            Score = Math.Min(1.0, (double)shortage / input.RequiredResourceCount),
            RequiresHumanReview = true
        });
    }
}

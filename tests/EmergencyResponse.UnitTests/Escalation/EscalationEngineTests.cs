using EmergencyResponse.Application.Escalation;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Escalation;

public class EscalationEngineTests
{
    [Fact]
    public void Evaluate_StableIncident_ReturnsNoRecommendations()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            CurrentSeverity = SeverityLevel.Low,
            PreviousSeverity = SeverityLevel.Low,
            PeopleTrapped = false,
            ImmediateDanger = false,
            CriticalInfrastructureAffected = false,
            Casualties = 0,
            RelatedIncidentCount = 0,
            DuplicateIncidentCount = 0,
            RequiredResourceCount = 2,
            AvailableMatchingResourceCount = 3
        };

        var recommendations = EscalationEngine.Evaluate(input);

        Assert.Empty(recommendations);
    }

    [Fact]
    public void Evaluate_SeverityIncrease_ProducesSeverityIncreaseEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            CurrentSeverity = SeverityLevel.Critical,
            PreviousSeverity = SeverityLevel.Moderate
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.SeverityIncrease);
        Assert.Equal(EscalationLevel.Critical, rec.Level);
        Assert.Contains("severity increased", rec.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.True(rec.RequiresHumanReview);
    }

    [Fact]
    public void Evaluate_PeopleTrapped_ProducesPeopleInDangerEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            PeopleTrapped = true
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.PeopleInDanger);
        Assert.Equal(EscalationLevel.Critical, rec.Level);
        Assert.Contains("trapped", rec.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_ImmediateDanger_ProducesCriticalEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            ImmediateDanger = true
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.ImmediateDanger);
        Assert.Equal(EscalationLevel.Critical, rec.Level);
        Assert.Contains("Immediate danger", rec.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_CriticalInfrastructureAffected_ProducesHighEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            CriticalInfrastructureAffected = true
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.CriticalInfrastructure);
        Assert.Equal(EscalationLevel.High, rec.Level);
        Assert.Contains("Critical infrastructure", rec.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_CasualtyCountGreaterOrEqualToFive_ProducesCriticalCasualtyIncreaseEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            Casualties = 7
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.CasualtyIncrease);
        Assert.Equal(EscalationLevel.Critical, rec.Level);
        Assert.Contains("7 casualties", rec.Reason);
    }

    [Fact]
    public void Evaluate_RelatedIncidentsGreaterOrEqualToThree_ProducesIncidentClusterEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            RelatedIncidentCount = 4
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.IncidentCluster);
        Assert.Equal(EscalationLevel.Elevated, rec.Level);
        Assert.Contains("4 related incidents", rec.Reason);
    }

    [Fact]
    public void Evaluate_DuplicateIncidentsGreaterOrEqualToThree_ProducesDuplicateClusterEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            DuplicateIncidentCount = 5
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.DuplicateIncidentCluster);
        Assert.Equal(EscalationLevel.Elevated, rec.Level);
        Assert.Contains("5 duplicate reports", rec.Reason);
    }

    [Fact]
    public void Evaluate_ResourceShortage_ProducesResourceShortageEscalation()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            RequiredResourceCount = 5,
            AvailableMatchingResourceCount = 2
        };

        var recommendations = EscalationEngine.Evaluate(input);

        var rec = Assert.Single(recommendations, r => r.Type == EscalationType.ResourceShortage);
        Assert.Equal(EscalationLevel.High, rec.Level);
        Assert.Contains("Required resources exceed", rec.Reason);
    }

    [Fact]
    public void Evaluate_MultipleTriggers_ProducesMultipleIndependentEscalations()
    {
        var input = new EscalationEvaluationInput
        {
            IncidentId = Guid.NewGuid(),
            PeopleTrapped = true,
            ImmediateDanger = true,
            Casualties = 10,
            CriticalInfrastructureAffected = true,
            RequiredResourceCount = 6,
            AvailableMatchingResourceCount = 1
        };

        var recommendations = EscalationEngine.Evaluate(input);

        Assert.Equal(5, recommendations.Count);
        Assert.Contains(recommendations, r => r.Type == EscalationType.PeopleInDanger);
        Assert.Contains(recommendations, r => r.Type == EscalationType.ImmediateDanger);
        Assert.Contains(recommendations, r => r.Type == EscalationType.CasualtyIncrease);
        Assert.Contains(recommendations, r => r.Type == EscalationType.CriticalInfrastructure);
        Assert.Contains(recommendations, r => r.Type == EscalationType.ResourceShortage);
    }
}

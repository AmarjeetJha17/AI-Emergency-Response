using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Decision;

namespace EmergencyResponse.UnitTests.Decision;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public class PriorityEngineTests
{
    [Fact]
    public void Critical_Severity_Should_Have_High_Base_Priority()
    {
        var engine = new PriorityEngine();

        var result = engine.Evaluate(
            new PriorityEvaluationInput
            {
                Severity = SeverityLevel.Critical
            });

        Assert.Equal(
            PriorityLevel.High,
            result.Priority);
    }

    [Fact]
    public void Critical_Severity_With_Trapped_People_Should_Be_Critical()
    {
        var engine = new PriorityEngine();

        var result = engine.Evaluate(
            new PriorityEvaluationInput
            {
                Severity = SeverityLevel.Critical,
                PeopleTrapped = true,
                Casualties = 1
            });

        Assert.Equal(
            PriorityLevel.Critical,
            result.Priority);
    }

    [Theory]
    [InlineData(SeverityLevel.Low)]
    [InlineData(SeverityLevel.Moderate)]
    [InlineData(SeverityLevel.High)]
    [InlineData(SeverityLevel.Critical)]
    public void PriorityScore_IsWithinRange_ForEverySeverity(SeverityLevel severity)
    {
        var result = new PriorityEngine().Evaluate(new PriorityEvaluationInput
        {
            Severity = severity,
            PeopleTrapped = true,
            Casualties = 3,
            PeopleAffected = 25,
            RoadBlocked = true,
            CriticalInfrastructureAffected = true,
            EscalationRisk = true,
            EmergencyServicesRequired = true
        });

        Assert.InRange(result.Score, 0, 100);
    }

    [Theory]
    [InlineData(SeverityLevel.Low, false, true, 0, false, false, PriorityLevel.Normal, 25)]
    [InlineData(SeverityLevel.High, false, false, 0, true, false, PriorityLevel.High, 50)]
    [InlineData(SeverityLevel.High, false, false, 20, true, true, PriorityLevel.Critical, 75)]
    [InlineData(SeverityLevel.Critical, false, false, 0, true, false, PriorityLevel.High, 70)]
    [InlineData(SeverityLevel.Critical, false, false, 20, false, false, PriorityLevel.Critical, 75)]
    public void PriorityThresholds_AreAppliedToBoundaryCombinations(
        SeverityLevel severity,
        bool peopleTrapped,
        bool casualty,
        int peopleAffected,
        bool roadBlocked,
        bool emergencyServicesRequired,
        PriorityLevel expected,
        int expectedScore)
    {
        var result = new PriorityEngine().Evaluate(new PriorityEvaluationInput
        {
            Severity = severity,
            PeopleTrapped = peopleTrapped,
            Casualties = casualty ? 1 : 0,
            PeopleAffected = peopleAffected,
            RoadBlocked = roadBlocked,
            EmergencyServicesRequired = emergencyServicesRequired
        });

        Assert.Equal(expected, result.Priority);
        Assert.Equal(expectedScore, result.Score);
    }

    [Fact]
    public void IncreasingCasualties_DoesNotReducePriorityScore()
    {
        var engine = new PriorityEngine();
        var scores = Enumerable.Range(0, 4)
            .Select(casualties => engine.Evaluate(new PriorityEvaluationInput
            {
                Severity = SeverityLevel.High,
                Casualties = casualties
            }).Score)
            .ToArray();

        Assert.Equal(scores.Order(), scores);
    }
}

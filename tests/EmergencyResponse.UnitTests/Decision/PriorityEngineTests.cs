using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Decision;

namespace EmergencyResponse.UnitTests.Decision;

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
}
using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Decision;

namespace EmergencyResponse.UnitTests.Decision;

public class SeverityEngineTests
{
    [Fact]
    public void No_Impact_Should_Be_Low()
    {
        var engine = new SeverityEngine();

        var result = engine.Evaluate(
            new SeverityEvaluationInput());

        Assert.Equal(
            SeverityLevel.Low,
            result.Severity);

        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Casualty_Should_Raise_Severity()
    {
        var engine = new SeverityEngine();

        var result = engine.Evaluate(
            new SeverityEvaluationInput
            {
                Casualties = 1
            });

        Assert.Equal(
            SeverityLevel.High,
            result.Severity);
    }

    [Fact]
    public void Trapped_People_And_Multiple_Impacts_Should_Be_Critical()
    {
        var engine = new SeverityEngine();

        var result = engine.Evaluate(
            new SeverityEvaluationInput
            {
                PeopleAffected = 10,
                VehiclesAffected = 3,
                PeopleTrapped = true,
                ResidentialAreaAffected = true,
                RoadBlocked = true,
                ImmediateDanger = true
            });

        Assert.Equal(
            SeverityLevel.Critical,
            result.Severity);

        Assert.True(result.Score >= 75);
    }
}
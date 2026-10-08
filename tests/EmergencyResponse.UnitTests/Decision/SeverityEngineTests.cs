using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Decision;

namespace EmergencyResponse.UnitTests.Decision;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(25)]
    [InlineData(49)]
    [InlineData(50)]
    [InlineData(74)]
    [InlineData(75)]
    [InlineData(100)]
    [InlineData(130)]
    public void Score_IsAlwaysWithinZeroAndOneHundred(int casualties)
    {
        var result = new SeverityEngine().Evaluate(new SeverityEvaluationInput
        {
            Casualties = casualties
        });

        Assert.InRange(result.Score, 0, 100);
    }

    [Fact]
    public void IncreasingCasualties_DoesNotReduceSeverityScore()
    {
        var engine = new SeverityEngine();
        var scores = Enumerable.Range(0, 20)
            .Select(casualties => engine.Evaluate(new SeverityEvaluationInput
            {
                Casualties = casualties
            }).Score)
            .ToArray();

        Assert.Equal(scores.Order(), scores);
    }

    [Fact]
    public void ImmediateDanger_DoesNotReduceSeverityScore()
    {
        var engine = new SeverityEngine();
        var withoutDanger = engine.Evaluate(new SeverityEvaluationInput
        {
            PeopleAffected = 10,
            VehiclesAffected = 2
        });
        var withDanger = engine.Evaluate(new SeverityEvaluationInput
        {
            PeopleAffected = 10,
            VehiclesAffected = 2,
            ImmediateDanger = true
        });

        Assert.True(withDanger.Score >= withoutDanger.Score);
    }
}

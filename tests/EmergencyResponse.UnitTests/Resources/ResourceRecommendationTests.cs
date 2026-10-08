using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

using Xunit;

namespace EmergencyResponse.UnitTests.Resources;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class ResourceRecommendationTests
{
    [Fact]
    public void SameLocation_ShouldReceiveMaximumDistanceScore()
    {
        var score = ResourceDistanceScorer.Calculate(0.0, 50.0);
        Assert.Equal(1.0, score);
    }

    [Fact]
    public void ResourceBeyondMaximumDistance_ShouldReceiveZero()
    {
        var score = ResourceDistanceScorer.Calculate(50.0, 50.0);
        Assert.Equal(0.0, score);

        var scoreFar = ResourceDistanceScorer.Calculate(75.0, 50.0);
        Assert.Equal(0.0, scoreFar);
    }

    [Fact]
    public void ResourceHalfway_ShouldReceiveHalfScore()
    {
        var score = ResourceDistanceScorer.Calculate(25.0, 50.0);
        Assert.Equal(0.5, score, precision: 4);
    }

    [Fact]
    public void NullDistance_ShouldReceiveZeroScore()
    {
        var score = ResourceDistanceScorer.Calculate(null, 50.0);
        Assert.Equal(0.0, score);
    }

    [Fact]
    public void NegativeDistance_ShouldReceiveMaximumScore()
    {
        var score = ResourceDistanceScorer.Calculate(-5.0, 50.0);
        Assert.Equal(1.0, score);
    }

    [Fact]
    public void CustomMaximumDistance_ShouldScaleLinearly()
    {
        var score = ResourceDistanceScorer.Calculate(10.0, 20.0);
        Assert.Equal(0.5, score, precision: 4);
    }

    [Fact]
    public void AllMaximumFactors_ShouldProduce100()
    {
        var score = ResourceRecommendationScorer.Calculate(
            distanceScore: 1.0,
            availabilityScore: 1.0,
            departmentScore: 1.0,
            suitabilityScore: 1.0);

        Assert.Equal(100.0, score);
    }

    [Fact]
    public void ZeroFactors_ShouldProduceZero()
    {
        var score = ResourceRecommendationScorer.Calculate(
            distanceScore: 0.0,
            availabilityScore: 0.0,
            departmentScore: 0.0,
            suitabilityScore: 0.0);

        Assert.Equal(0.0, score);
    }

    [Theory]
    [InlineData(1.0, 0.0, 0.0, 0.0, 50.0)] // 50% distance
    [InlineData(0.0, 1.0, 0.0, 0.0, 20.0)] // 20% availability
    [InlineData(0.0, 0.0, 1.0, 0.0, 20.0)] // 20% department
    [InlineData(0.0, 0.0, 0.0, 1.0, 10.0)] // 10% suitability
    public void FactorWeights_ReflectIndividualContributions(
        double distance,
        double availability,
        double department,
        double suitability,
        double expectedScore)
    {
        var score = ResourceRecommendationScorer.Calculate(
            distance,
            availability,
            department,
            suitability);

        Assert.Equal(expectedScore, score);
    }

    [Fact]
    public void ValidRequirement_PassesValidation()
    {
        var requirement = new IncidentTypeResourceRequirement
        {
            Id = Guid.NewGuid(),
            IncidentTypeId = Guid.NewGuid(),
            ResourceType = ResourceType.FireTruck,
            MinimumCount = 1,
            PreferredCount = 2,
            PriorityOrder = 1
        };

        var exception = Record.Exception(() => IncidentTypeResourceRequirementValidator.Validate(requirement));
        Assert.Null(exception);
    }

    [Fact]
    public void PreferredCountBelowMinimum_ThrowsArgumentException()
    {
        var requirement = new IncidentTypeResourceRequirement
        {
            MinimumCount = 3,
            PreferredCount = 1,
            PriorityOrder = 1
        };

        Assert.Throws<ArgumentException>(() => IncidentTypeResourceRequirementValidator.Validate(requirement));
    }

    [Fact]
    public void NegativeMinimumCount_ThrowsArgumentOutOfRangeException()
    {
        var requirement = new IncidentTypeResourceRequirement
        {
            MinimumCount = -1,
            PreferredCount = 2,
            PriorityOrder = 1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => IncidentTypeResourceRequirementValidator.Validate(requirement));
    }

    [Fact]
    public void ZeroPriorityOrder_ThrowsArgumentOutOfRangeException()
    {
        var requirement = new IncidentTypeResourceRequirement
        {
            MinimumCount = 1,
            PreferredCount = 1,
            PriorityOrder = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => IncidentTypeResourceRequirementValidator.Validate(requirement));
    }
}

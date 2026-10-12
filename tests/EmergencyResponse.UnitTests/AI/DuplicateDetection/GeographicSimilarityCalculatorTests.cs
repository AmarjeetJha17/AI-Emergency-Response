using EmergencyResponse.Application.AI.DuplicateDetection;

namespace EmergencyResponse.UnitTests.AI.DuplicateDetection;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class GeographicSimilarityCalculatorTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-2, 1)]
    public void Calculate_ReturnsMaximumSimilarityForZeroOrNegativeDistance(
        double distanceKm,
        double expected)
    {
        Assert.Equal(expected, GeographicSimilarityCalculator.Calculate(distanceKm));
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(12, 0)]
    public void Calculate_ReturnsZeroAtOrBeyondRelevantDistance(
        double distanceKm,
        double expected)
    {
        Assert.Equal(expected, GeographicSimilarityCalculator.Calculate(distanceKm));
    }

    [Fact]
    public void Calculate_DecreasesLinearlyInsideRelevantDistance()
    {
        Assert.Equal(0.5, GeographicSimilarityCalculator.Calculate(5));
        Assert.Equal(0.75, GeographicSimilarityCalculator.Calculate(5, maximumRelevantDistanceKm: 20));
    }
}

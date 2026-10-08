
using EmergencyResponse.Application.AI.DuplicateDetection;

namespace EmergencyResponse.UnitTests.AI;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public class DuplicateScoringTests
{
    [Fact]
    public void IdenticalSignals_ShouldProduceOne()
    {
        var score = DuplicateScoreCalculator.Calculate(
            1.0,
            1.0,
            1.0,
            1.0);

        Assert.Equal(1.0, score);
    }

    [Fact]
    public void ZeroSignals_ShouldProduceZero()
    {
        var score = DuplicateScoreCalculator.Calculate(
            0.0,
            0.0,
            0.0,
            0.0);

        Assert.Equal(0.0, score);
    }

    [Fact]
    public void SemanticSignal_ShouldHaveHighestWeight()
    {
        var score = DuplicateScoreCalculator.Calculate(
            1.0,
            0.0,
            0.0,
            0.0);

        Assert.Equal(0.50, score);
    }

    [Fact]
    public void SameTime_ShouldHaveMaximumTemporalSimilarity()
    {
        var similarity =
            TemporalSimilarityCalculator.Calculate(
                TimeSpan.Zero);

        Assert.Equal(1.0, similarity);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.74)]
    [InlineData(0.75)]
    [InlineData(0.89)]
    [InlineData(0.90)]
    [InlineData(1.00)]
    public void DuplicateScore_BoundaryValues_ArePreserved(double signal)
    {
        var score = DuplicateScoreCalculator.Calculate(signal, signal, signal, signal);

        Assert.Equal(signal, score, precision: 8);
        Assert.InRange(score, 0d, 1d);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.0, 1.0)]
    public void DuplicateScore_ClampsOutOfRangeSignals(double signal, double expected)
    {
        Assert.Equal(expected, DuplicateScoreCalculator.Calculate(signal, signal, signal, signal));
    }
}

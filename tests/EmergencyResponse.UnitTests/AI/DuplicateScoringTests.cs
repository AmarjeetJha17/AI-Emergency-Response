
using EmergencyResponse.Application.AI.DuplicateDetection;

namespace EmergencyResponse.UnitTests.AI;

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
}

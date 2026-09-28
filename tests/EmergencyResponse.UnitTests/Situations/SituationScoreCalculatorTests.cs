using EmergencyResponse.Application.Situations;
using Xunit;

namespace EmergencyResponse.UnitTests.Situations;

public class SituationScoreCalculatorTests
{
    [Fact]
    public void Calculate_WithZeroMetrics_ReturnsZero()
    {
        var score = SituationScoreCalculator.Calculate(0, 0, 0, 0, 0);
        Assert.Equal(0.0, score);
    }

    [Fact]
    public void Calculate_WithMaximumSaturation_ReturnsHundred()
    {
        // 5 critical (0.40) + 10 high (0.25) + 20 incidents (0.15) + 100 people (0.10) + 20 casualties (0.10) = 1.00 -> 100.0
        var score = SituationScoreCalculator.Calculate(20, 5, 10, 100, 20);
        Assert.Equal(100.0, score);
    }

    [Fact]
    public void Calculate_WithModerateInputs_ProducesExpectedProportionalScore()
    {
        // 1 critical: (1/5) * 0.40 = 0.08
        // 2 high: (2/10) * 0.25 = 0.05
        // 5 incidents: (5/20) * 0.15 = 0.0375
        // 20 people: (20/100) * 0.10 = 0.02
        // 2 casualties: (2/20) * 0.10 = 0.01
        // sum = 0.1975 -> 19.75
        var score = SituationScoreCalculator.Calculate(5, 1, 2, 20, 2);
        Assert.Equal(19.75, score, precision: 2);
    }

    [Fact]
    public void Calculate_ValuesExceedingSaturation_ClampsToHundred()
    {
        var score = SituationScoreCalculator.Calculate(100, 50, 50, 500, 100);
        Assert.Equal(100.0, score);
    }
}

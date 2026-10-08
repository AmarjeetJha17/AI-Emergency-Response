using EmergencyResponse.Application.Situations;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Situations;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public class SituationLevelCalculatorTests
{
    [Theory]
    [InlineData(100.0, EscalationLevel.Critical)]
    [InlineData(75.0, EscalationLevel.Critical)]
    [InlineData(74.9, EscalationLevel.High)]
    [InlineData(50.0, EscalationLevel.High)]
    [InlineData(49.9, EscalationLevel.Elevated)]
    [InlineData(25.0, EscalationLevel.Elevated)]
    [InlineData(24.9, EscalationLevel.Advisory)]
    [InlineData(0.0, EscalationLevel.Advisory)]
    public void Calculate_MapsScoreToExpectedEscalationLevel(double score, EscalationLevel expectedLevel)
    {
        var level = SituationLevelCalculator.Calculate(score);
        Assert.Equal(expectedLevel, level);
    }
}

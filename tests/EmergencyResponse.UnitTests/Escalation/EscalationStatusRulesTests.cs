using EmergencyResponse.Application.Escalation;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Escalation;

public class EscalationStatusRulesTests
{
    [Theory]
    [InlineData(EscalationStatus.Open, EscalationStatus.Acknowledged, true)]
    [InlineData(EscalationStatus.Open, EscalationStatus.Resolved, true)]
    [InlineData(EscalationStatus.Open, EscalationStatus.Dismissed, true)]
    [InlineData(EscalationStatus.Acknowledged, EscalationStatus.Resolved, true)]
    [InlineData(EscalationStatus.Acknowledged, EscalationStatus.Dismissed, true)]
    public void IsAllowed_ValidTransitions_ReturnsTrue(
        EscalationStatus from,
        EscalationStatus to,
        bool expected)
    {
        var result = EscalationStatusRules.IsAllowed(from, to);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(EscalationStatus.Open, EscalationStatus.Open)]
    [InlineData(EscalationStatus.Acknowledged, EscalationStatus.Open)]
    [InlineData(EscalationStatus.Acknowledged, EscalationStatus.Acknowledged)]
    [InlineData(EscalationStatus.Resolved, EscalationStatus.Open)]
    [InlineData(EscalationStatus.Resolved, EscalationStatus.Acknowledged)]
    [InlineData(EscalationStatus.Resolved, EscalationStatus.Resolved)]
    [InlineData(EscalationStatus.Resolved, EscalationStatus.Dismissed)]
    [InlineData(EscalationStatus.Dismissed, EscalationStatus.Open)]
    [InlineData(EscalationStatus.Dismissed, EscalationStatus.Acknowledged)]
    [InlineData(EscalationStatus.Dismissed, EscalationStatus.Resolved)]
    [InlineData(EscalationStatus.Dismissed, EscalationStatus.Dismissed)]
    public void IsAllowed_InvalidTransitions_ReturnsFalse(
        EscalationStatus from,
        EscalationStatus to)
    {
        var result = EscalationStatusRules.IsAllowed(from, to);
        Assert.False(result);
    }
}

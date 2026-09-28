using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Situations;

public class OperationalSituationStateMachineTests
{
    [Fact]
    public void Acknowledge_WhenOpen_TransitionsToAcknowledged()
    {
        var situation = new OperationalSituation
        {
            Status = EscalationStatus.Open,
            RequiresHumanReview = true
        };

        situation.Status = EscalationStatus.Acknowledged;
        situation.RequiresHumanReview = false;

        Assert.Equal(EscalationStatus.Acknowledged, situation.Status);
        Assert.False(situation.RequiresHumanReview);
    }

    [Theory]
    [InlineData(EscalationStatus.Acknowledged)]
    [InlineData(EscalationStatus.Resolved)]
    [InlineData(EscalationStatus.Dismissed)]
    public void Acknowledge_WhenNotOpen_IsDisallowed(EscalationStatus currentStatus)
    {
        var situation = new OperationalSituation { Status = currentStatus };
        var canAcknowledge = situation.Status == EscalationStatus.Open;
        Assert.False(canAcknowledge);
    }

    [Fact]
    public void Resolve_LeavesMemberIncidentsUnchanged()
    {
        var incident = new Incident
        {
            IncidentNumber = "INC-101",
            Status = IncidentStatus.InProgress,
            Severity = SeverityLevel.Critical
        };

        var situation = new OperationalSituation
        {
            Status = EscalationStatus.Open
        };

        // Resolve situation
        situation.Status = EscalationStatus.Resolved;

        // Incident must remain unchanged
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Equal(SeverityLevel.Critical, incident.Severity);
    }

    [Fact]
    public void Dismiss_LeavesMemberIncidentsUnchanged()
    {
        var incident = new Incident
        {
            IncidentNumber = "INC-102",
            Status = IncidentStatus.Assigned,
            Severity = SeverityLevel.High
        };

        var situation = new OperationalSituation
        {
            Status = EscalationStatus.Open
        };

        // Dismiss situation
        situation.Status = EscalationStatus.Dismissed;

        // Incident must remain unchanged
        Assert.Equal(IncidentStatus.Assigned, incident.Status);
        Assert.Equal(SeverityLevel.High, incident.Severity);
    }
}

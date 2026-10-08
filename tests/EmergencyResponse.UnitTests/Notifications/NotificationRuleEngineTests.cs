using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;
using Xunit;

namespace EmergencyResponse.UnitTests.Notifications;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class NotificationRuleEngineTests
{
    private readonly NotificationRuleEngine _engine = new();

    [Fact]
    public void Evaluate_EscalationOpened_CriticalLevel_ReturnsCriticalPriorityNotification()
    {
        var incidentId = Guid.NewGuid();
        var escalationId = Guid.NewGuid();

        var context = new NotificationContext
        {
            IncidentId = incidentId,
            IncidentNumber = "INC-2026-001",
            EscalationId = escalationId,
            EscalationType = EscalationType.SeverityIncrease,
            EscalationLevel = EscalationLevel.Critical,
            Reason = "Severity increased to Critical with life safety hazard"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests, r => r.Type == NotificationType.EscalationOpened);
        Assert.Equal(NotificationPriority.Critical, request.Priority);
        Assert.Contains("SeverityIncrease", request.Title);
        Assert.Contains("Critical", request.Title);
        Assert.Contains("life safety hazard", request.Message);
        Assert.Equal(incidentId, request.IncidentId);
        Assert.Equal(escalationId, request.IncidentEscalationId);
        Assert.Equal($"EscalationOpened:{escalationId}", request.DeduplicationKey);
        Assert.Equal(ApplicationRoles.Operator, request.TargetRole);
    }

    [Theory]
    [InlineData(EscalationLevel.Critical, NotificationPriority.Critical)]
    [InlineData(EscalationLevel.High, NotificationPriority.Urgent)]
    [InlineData(EscalationLevel.Elevated, NotificationPriority.Important)]
    [InlineData(EscalationLevel.Advisory, NotificationPriority.Normal)]
    public void Evaluate_EscalationLevels_MapsToExpectedPriorities(
        EscalationLevel level,
        NotificationPriority expectedPriority)
    {
        var context = new NotificationContext
        {
            IncidentId = Guid.NewGuid(),
            EscalationId = Guid.NewGuid(),
            EscalationType = EscalationType.ImmediateDanger,
            EscalationLevel = level,
            Reason = "Escalation evaluation"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests);
        Assert.Equal(expectedPriority, request.Priority);
    }

    [Fact]
    public void Evaluate_PeopleTrapped_ReturnsCriticalAlert()
    {
        var incidentId = Guid.NewGuid();
        var context = new NotificationContext
        {
            IncidentId = incidentId,
            IncidentNumber = "INC-100",
            PeopleTrapped = true,
            Details = "2 people trapped on 2nd floor"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests, r => r.DeduplicationKey == $"PeopleTrapped:{incidentId}");
        Assert.Equal(NotificationPriority.Critical, request.Priority);
        Assert.Contains("People Trapped", request.Title);
        Assert.Contains("INC-100", request.Title);
        Assert.Equal(ApplicationRoles.Operator, request.TargetRole);
    }

    [Fact]
    public void Evaluate_ImmediateDanger_ReturnsUrgentAlert()
    {
        var incidentId = Guid.NewGuid();
        var context = new NotificationContext
        {
            IncidentId = incidentId,
            IncidentNumber = "INC-101",
            ImmediateDanger = true,
            Details = "Active gas leak near furnace"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests, r => r.DeduplicationKey == $"ImmediateDanger:{incidentId}");
        Assert.Equal(NotificationPriority.Urgent, request.Priority);
        Assert.Contains("Immediate Danger", request.Title);
        Assert.Contains("INC-101", request.Title);
    }

    [Fact]
    public void Evaluate_CriticalSituation_ReturnsCriticalNotification()
    {
        var situationId = Guid.NewGuid();
        var context = new NotificationContext
        {
            SituationId = situationId,
            SituationNumber = "SIT-0001",
            SituationLevel = EscalationLevel.Critical,
            Reason = "Multiple flash floods merging"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests, r => r.Type == NotificationType.SituationLevelChanged);
        Assert.Equal(NotificationPriority.Critical, request.Priority);
        Assert.Contains("CRITICAL SITUATION", request.Title);
        Assert.Contains("SIT-0001", request.Title);
        Assert.Equal(situationId, request.OperationalSituationId);
        Assert.Equal($"CriticalSituation:{situationId}", request.DeduplicationKey);
    }

    [Fact]
    public void Evaluate_ElevatedSituation_ReturnsSituationCreated()
    {
        var situationId = Guid.NewGuid();
        var context = new NotificationContext
        {
            SituationId = situationId,
            SituationNumber = "SIT-0002",
            SituationLevel = EscalationLevel.Elevated,
            Reason = "Power outage across three blocks"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests, r => r.Type == NotificationType.SituationCreated);
        Assert.Equal(NotificationPriority.Important, request.Priority);
        Assert.Contains("Operational Situation", request.Title);
        Assert.Equal($"SituationCreated:{situationId}", request.DeduplicationKey);
    }

    [Fact]
    public void Evaluate_ResourceShortage_ReturnsUrgentAlert()
    {
        var situationId = Guid.NewGuid();
        var context = new NotificationContext
        {
            SituationId = situationId,
            ResourceShortage = true,
            Details = "Missing hazmat decontamination trailer"
        };

        var requests = _engine.Evaluate(context);

        var request = Assert.Single(requests, r => r.Type == NotificationType.ResourceShortage);
        Assert.Equal(NotificationPriority.Urgent, request.Priority);
        Assert.Contains("Resource Shortage", request.Title);
        Assert.Equal($"ResourceShortage:{situationId}", request.DeduplicationKey);
    }

    [Fact]
    public void Evaluate_ReviewQueues_ReturnsImportantAlertsForEachType()
    {
        var incidentId = Guid.NewGuid();
        var context = new NotificationContext
        {
            IncidentId = incidentId,
            IncidentNumber = "INC-300",
            RequiresAIReview = true,
            RequiresDuplicateReview = true,
            RequiresVisualReview = true,
            Details = "Reviews pending"
        };

        var requests = _engine.Evaluate(context);

        Assert.Equal(3, requests.Count);
        Assert.Contains(requests, r => r.Type == NotificationType.AIReviewRequired && r.Priority == NotificationPriority.Important);
        Assert.Contains(requests, r => r.Type == NotificationType.DuplicateReviewRequired && r.Priority == NotificationPriority.Important);
        Assert.Contains(requests, r => r.Type == NotificationType.VisualReviewRequired && r.Priority == NotificationPriority.Important);
    }
}

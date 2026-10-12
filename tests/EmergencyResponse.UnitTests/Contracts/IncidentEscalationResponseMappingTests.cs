using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Escalation;
using EmergencyResponse.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EmergencyResponse.UnitTests.Contracts;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class IncidentEscalationResponseMappingTests
{
    [Fact]
    public async Task GetByIncidentId_maps_all_escalation_fields_and_evidence_fallbacks()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Flood",
            Code = "FLOOD",
            IsActive = true
        };
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-2026-0102",
            Title = "Flooded bridge",
            Description = "Water is crossing the bridge.",
            IncidentTypeId = incidentType.Id,
            IncidentType = incidentType,
            Status = IncidentStatus.Escalated,
            Severity = SeverityLevel.High,
            Priority = PriorityLevel.High,
            ReportedAtUtc = new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc)
        };
        var validEvidenceId = Guid.NewGuid();
        var malformedEvidenceId = Guid.NewGuid();
        var emptyEvidenceId = Guid.NewGuid();
        fixture.Context.IncidentTypes.Add(incidentType);
        fixture.Context.Incidents.Add(incident);
        fixture.Context.IncidentEscalations.AddRange(
            new IncidentEscalation
            {
                Id = validEvidenceId,
                IncidentId = incident.Id,
                Type = EscalationType.ResourceShortage,
                Level = EscalationLevel.Critical,
                Status = EscalationStatus.Acknowledged,
                Reason = "Specialist equipment is unavailable.",
                EvidenceJson = "[\"no rescue boat\",\"water depth 1.4m\"]",
                Score = 0.97,
                RequiresHumanReview = true,
                AcknowledgedByUserId = "operator-1",
                AcknowledgedAtUtc = Utc(8, 20),
                ResolvedByUserId = "supervisor-2",
                ResolvedAtUtc = Utc(8, 45),
                ResolutionNotes = "Boat dispatched from the north station.",
                CreatedAtUtc = Utc(8, 10)
            },
            new IncidentEscalation
            {
                Id = malformedEvidenceId,
                IncidentId = incident.Id,
                Type = EscalationType.SeverityIncrease,
                Level = EscalationLevel.High,
                Status = EscalationStatus.Open,
                Reason = "Evidence payload is legacy text.",
                EvidenceJson = "legacy evidence payload",
                Score = 0.82,
                RequiresHumanReview = false,
                CreatedAtUtc = Utc(8, 5)
            },
            new IncidentEscalation
            {
                Id = emptyEvidenceId,
                IncidentId = incident.Id,
                Type = (EscalationType)0,
                Level = EscalationLevel.Advisory,
                Status = EscalationStatus.Resolved,
                Reason = "No evidence recorded.",
                EvidenceJson = "  ",
                CreatedAtUtc = Utc(8, 1)
            });
        await fixture.Context.SaveChangesAsync();

        var service = new IncidentEscalationService(
            fixture.Context,
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IIncidentNotificationService>(),
            NullLogger<IncidentEscalationService>.Instance);

        var responses = await service.GetByIncidentIdAsync(incident.Id);

        Assert.Equal(3, responses.Count);
        var valid = Assert.Single(responses, response => response.Id == validEvidenceId);
        Assert.Equal(incident.Id, valid.IncidentId);
        Assert.Equal("INC-2026-0102", valid.IncidentNumber);
        Assert.Equal("Flooded bridge", valid.IncidentTitle);
        Assert.Equal(EscalationType.ResourceShortage, valid.Type);
        Assert.Equal("Resource Shortage", valid.TypeName);
        Assert.Equal(EscalationLevel.Critical, valid.Level);
        Assert.Equal("Critical", valid.LevelName);
        Assert.Equal(EscalationStatus.Acknowledged, valid.Status);
        Assert.Equal("Acknowledged", valid.StatusName);
        Assert.Equal("Specialist equipment is unavailable.", valid.Reason);
        Assert.Equal(new[] { "no rescue boat", "water depth 1.4m" }, valid.Evidence);
        Assert.Equal(0.97, valid.Score);
        Assert.True(valid.RequiresHumanReview);
        Assert.Equal("operator-1", valid.AcknowledgedByUserId);
        Assert.Equal("operator-1", valid.AcknowledgedByUserName);
        Assert.Equal(Utc(8, 20), valid.AcknowledgedAtUtc);
        Assert.Equal("supervisor-2", valid.ResolvedByUserId);
        Assert.Equal("supervisor-2", valid.ResolvedByUserName);
        Assert.Equal(Utc(8, 45), valid.ResolvedAtUtc);
        Assert.Equal("Boat dispatched from the north station.", valid.ResolutionNotes);
        Assert.NotEqual(default, valid.CreatedAtUtc);

        var malformed = Assert.Single(responses, response => response.Id == malformedEvidenceId);
        Assert.Equal(new[] { "legacy evidence payload" }, malformed.Evidence);

        var empty = Assert.Single(responses, response => response.Id == emptyEvidenceId);
        Assert.Empty(empty.Evidence);
        Assert.Equal("0", empty.TypeName);
    }

    private static DateTime Utc(int hour, int minute)
        => new(2026, 10, 12, hour, minute, 0, DateTimeKind.Utc);
}

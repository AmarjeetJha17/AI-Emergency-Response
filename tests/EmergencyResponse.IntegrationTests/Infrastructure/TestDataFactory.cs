using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public static class TestDataFactory
{
    public static Incident CreateIncident(
        Guid incidentTypeId,
        string? reporterUserId = null,
        string title = "Integration test incident",
        string incidentNumber = "TEST-INC-001")
        => new()
        {
            Id = Guid.NewGuid(),
            IncidentNumber = incidentNumber,
            Title = title,
            Description = "Deterministic integration-test emergency report.",
            IncidentTypeId = incidentTypeId,
            ReporterUserId = reporterUserId,
            Status = IncidentStatus.Submitted,
            Severity = SeverityLevel.Low,
            Priority = PriorityLevel.Low,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    public static IncidentLocation CreateIncidentLocation(Guid incidentId)
        => new()
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Latitude = 18.5204m,
            Longitude = 73.8567m,
            Address = "Test Road",
            City = "Test City",
            District = "Test District",
            State = "Test State",
            IsPrimary = true
        };
}

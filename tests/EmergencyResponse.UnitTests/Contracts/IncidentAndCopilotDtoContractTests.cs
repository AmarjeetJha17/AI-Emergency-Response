using System.Text.Json;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Services;
using EmergencyResponse.UnitTests.Infrastructure;
using Moq;

namespace EmergencyResponse.UnitTests.Contracts;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class IncidentAndCopilotDtoContractTests
{
    [Fact]
    public async Task CreateIncidentRequest_MapsAllSubmittedFieldsToIncidentAndPrimaryLocation()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Fire",
            Code = "FIRE",
            IsActive = true
        };
        fixture.Context.IncidentTypes.Add(incidentType);
        await fixture.Context.SaveChangesAsync();

        var numberGenerator = new Mock<IIncidentNumberGenerator>(MockBehavior.Strict);
        numberGenerator.Setup(generator => generator.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("INC-UNIT-001");
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.Setup(user => user.UserId).Returns("reporter-1");
        var scheduler = new Mock<EmergencyResponse.Application.Reliability.IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(item => item.ScheduleIncidentAI(It.IsAny<Guid>())).Returns(Guid.NewGuid());
        scheduler.Setup(item => item.ScheduleDuplicateDetection(It.IsAny<Guid>())).Returns(Guid.NewGuid());
        scheduler.Setup(item => item.ScheduleExternalContext(It.IsAny<Guid>())).Returns(Guid.NewGuid());
        var service = new IncidentService(fixture.Context, numberGenerator.Object, currentUser.Object, scheduler.Object);
        var request = new CreateIncidentRequest
        {
            Title = "  Warehouse fire  ",
            Description = "  Smoke reported at loading bay.  ",
            IncidentTypeId = incidentType.Id,
            Latitude = 28.6139m,
            Longitude = 77.2090m,
            Address = "  12 Market Road  ",
            City = "  New Delhi  ",
            District = "  Central  ",
            State = "  Delhi  ",
            PeopleAffected = 12,
            VehiclesAffected = 3,
            Casualties = 1
        };

        var response = await service.CreateAsync(request);
        var saved = await fixture.Context.Incidents.FindAsync(response.Id);
        var location = Assert.Single(fixture.Context.IncidentLocations.Where(item => item.IncidentId == response.Id));

        Assert.NotNull(saved);
        Assert.Equal("INC-UNIT-001", response.IncidentNumber);
        Assert.Equal("Warehouse fire", saved.Title);
        Assert.Equal("Smoke reported at loading bay.", saved.Description);
        Assert.Equal("reporter-1", saved.ReporterUserId);
        Assert.Equal(incidentType.Id, saved.IncidentTypeId);
        Assert.Equal(IncidentStatus.Submitted, saved.Status);
        Assert.Equal(SeverityLevel.Low, saved.Severity);
        Assert.Equal(PriorityLevel.Normal, saved.Priority);
        Assert.Equal(12, saved.PeopleAffected);
        Assert.Equal(3, saved.VehiclesAffected);
        Assert.Equal(1, saved.Casualties);
        Assert.Equal(request.Latitude, location.Latitude);
        Assert.Equal(request.Longitude, location.Longitude);
        Assert.Equal("12 Market Road", location.Address);
        Assert.Equal("New Delhi", location.City);
        Assert.Equal("Central", location.District);
        Assert.Equal("Delhi", location.State);
        Assert.True(location.IsPrimary);
        Assert.Equal(request.Latitude, response.Latitude);
        Assert.Equal(request.Longitude, response.Longitude);
        Assert.Single(saved.StatusHistory);
        scheduler.Verify(item => item.ScheduleIncidentAI(response.Id), Times.Once);
        scheduler.Verify(item => item.ScheduleDuplicateDetection(response.Id), Times.Once);
        scheduler.Verify(item => item.ScheduleExternalContext(response.Id), Times.Once);
    }

    [Fact]
    public void DashboardUpdatedEvent_SystemTextJsonRoundTrip_PreservesRealtimeContract()
    {
        var expected = new DashboardUpdatedEvent
        {
            Reason = "AIAnalysisCompleted",
            IncidentId = Guid.NewGuid(),
            UpdatedAtUtc = new DateTime(2026, 10, 12, 12, 30, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(expected);
        var actual = JsonSerializer.Deserialize<DashboardUpdatedEvent>(json);

        Assert.NotNull(actual);
        Assert.Equal(expected.Reason, actual.Reason);
        Assert.Equal(expected.IncidentId, actual.IncidentId);
        Assert.Equal(expected.UpdatedAtUtc, actual.UpdatedAtUtc);
    }

    [Fact]
    public void CopilotPolicyValidationResult_SystemTextJsonRoundTrip_PreservesDecisionAndViolations()
    {
        var expected = new CopilotPolicyValidationResult
        {
            Allowed = false,
            Violations = ["Response contains an explicitly unsupported claim."]
        };

        var actual = JsonSerializer.Deserialize<CopilotPolicyValidationResult>(
            JsonSerializer.Serialize(expected));

        Assert.NotNull(actual);
        Assert.False(actual.Allowed);
        Assert.Equal(expected.Violations, actual.Violations);
    }

    [Fact]
    public void CopilotActionExecutionResult_SystemTextJsonRoundTrip_PreservesExecutionOutcome()
    {
        var expected = new CopilotActionExecutionResult(
            false,
            "The approved Copilot action could not be completed.",
            "ACTION_EXECUTION_FAILED",
            "{\"succeeded\":false}",
            new DateTime(2026, 10, 12, 12, 30, 0, DateTimeKind.Utc));

        var actual = JsonSerializer.Deserialize<CopilotActionExecutionResult>(
            JsonSerializer.Serialize(expected));

        Assert.Equal(expected, actual);
    }
}

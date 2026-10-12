using System.Text.Json;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Copilot;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Copilot.Actions;
using EmergencyResponse.UnitTests.Infrastructure;
using Moq;

namespace EmergencyResponse.UnitTests.Copilot.Actions;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class CopilotActionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenApprovedAnnotationSucceeds_TransitionsProposalToSucceeded()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var now = new DateTime(2026, 10, 12, 12, 30, 0, DateTimeKind.Utc);
        var incidentId = Guid.NewGuid();
        var proposal = CreateApprovedProposal(incidentId, "content", "Place a safety perimeter.");
        fixture.Context.CopilotActionProposals.Add(proposal);
        await fixture.Context.SaveChangesAsync();

        var annotations = new Mock<IIncidentAnnotationService>(MockBehavior.Strict);
        annotations
            .Setup(service => service.AddNoteAsync(
                incidentId,
                "Place a safety perimeter.",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncidentAnnotationResult(incidentId, "Place a safety perimeter.", now));
        var notifications = new Mock<ICopilotActionNotificationService>(MockBehavior.Strict);
        notifications
            .Setup(service => service.PublishAsync(
                It.Is<CopilotActionUpdatedEvent>(update =>
                    update.ProposalId == proposal.Id &&
                    update.Status == CopilotActionStatus.Succeeded),
                "operator-1",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var executor = CreateExecutor(fixture, now, annotations, notifications);

        var result = await executor.ExecuteAsync(proposal.Id);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Equal("The approved Copilot action completed successfully.", result.Message);
        Assert.Equal(now, result.ExecutedAtUtc);
        Assert.NotNull(result.ResultJson);
        var savedProposal = await fixture.Context.CopilotActionProposals.FindAsync(proposal.Id);
        Assert.NotNull(savedProposal);
        Assert.Equal(CopilotActionStatus.Succeeded, savedProposal.Status);
        Assert.Equal(now, savedProposal.ExecutedAtUtc);
        Assert.Null(savedProposal.FailureReason);
        Assert.Contains(fixture.Context.AuditLogs, audit => audit.Action == "COPILOT_ACTION_EXECUTING");
        Assert.Contains(fixture.Context.AuditLogs, audit => audit.Action == "COPILOT_ACTION_SUCCEEDED");
        annotations.Verify(service => service.AddNoteAsync(
            incidentId,
            "Place a safety perimeter.",
            It.IsAny<CancellationToken>()), Times.Once);
        notifications.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnnotationFails_TransitionsProposalToFailedAndReturnsSafeFailureResult()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var now = new DateTime(2026, 10, 12, 12, 30, 0, DateTimeKind.Utc);
        var incidentId = Guid.NewGuid();
        var proposal = CreateApprovedProposal(incidentId, "content", "Place a safety perimeter.");
        fixture.Context.CopilotActionProposals.Add(proposal);
        await fixture.Context.SaveChangesAsync();

        var annotations = new Mock<IIncidentAnnotationService>(MockBehavior.Strict);
        annotations
            .Setup(service => service.AddNoteAsync(
                incidentId,
                "Place a safety perimeter.",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("annotation provider unavailable"));
        var notifications = new Mock<ICopilotActionNotificationService>(MockBehavior.Strict);
        notifications
            .Setup(service => service.PublishAsync(
                It.Is<CopilotActionUpdatedEvent>(update =>
                    update.ProposalId == proposal.Id &&
                    update.Status == CopilotActionStatus.Failed),
                "operator-1",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var executor = CreateExecutor(fixture, now, annotations, notifications);

        var result = await executor.ExecuteAsync(proposal.Id);

        Assert.False(result.Succeeded);
        Assert.Equal("ACTION_EXECUTION_FAILED", result.ErrorCode);
        Assert.Equal("The approved Copilot action could not be completed.", result.Message);
        Assert.Equal(now, result.ExecutedAtUtc);
        Assert.NotNull(result.ResultJson);
        using var failurePayload = JsonDocument.Parse(result.ResultJson);
        Assert.False(failurePayload.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal("annotation provider unavailable", failurePayload.RootElement.GetProperty("error").GetString());

        var savedProposal = await fixture.Context.CopilotActionProposals.FindAsync(proposal.Id);
        Assert.NotNull(savedProposal);
        Assert.Equal(CopilotActionStatus.Failed, savedProposal.Status);
        Assert.Equal("annotation provider unavailable", savedProposal.FailureReason);
        Assert.Equal(now, savedProposal.ExecutedAtUtc);
        Assert.Contains(fixture.Context.AuditLogs, audit => audit.Action == "COPILOT_ACTION_EXECUTING");
        Assert.Contains(fixture.Context.AuditLogs, audit => audit.Action == "COPILOT_ACTION_FAILED");
        notifications.VerifyAll();
    }

    private static CopilotActionExecutor CreateExecutor(
        SqliteApplicationDbFixture fixture,
        DateTime now,
        Mock<IIncidentAnnotationService> annotations,
        Mock<ICopilotActionNotificationService> notifications)
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.Setup(user => user.UserId).Returns("operator-1");
        var timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(now));

        return new CopilotActionExecutor(
            fixture.Context,
            currentUser.Object,
            annotations.Object,
            notifications.Object,
            timeProvider.Object);
    }

    private static CopilotActionProposal CreateApprovedProposal(
        Guid incidentId,
        string argumentName,
        string argumentValue)
        => new()
        {
            Id = Guid.NewGuid(),
            CopilotInteractionId = Guid.NewGuid(),
            ActionType = CopilotActionType.CreateIncidentNote,
            RiskLevel = CopilotActionRiskLevel.Low,
            Status = CopilotActionStatus.Approved,
            Title = "Add an incident note",
            Description = "Record an operator-approved safety note.",
            ArgumentsJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                [argumentName] = argumentValue
            }),
            IncidentId = incidentId,
            ProposedByUserId = "operator-1",
            ProposedAtUtc = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc),
            ApprovalDecision = CopilotApprovalDecision.Approved,
            ApprovedByUserId = "operator-1",
            ApprovedAtUtc = new DateTime(2026, 10, 12, 12, 5, 0, DateTimeKind.Utc),
            CorrelationId = Guid.NewGuid().ToString("N")
        };
}

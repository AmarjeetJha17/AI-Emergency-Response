using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Decision.DTOs;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Application.Escalation;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Predictive.Interfaces;
using EmergencyResponse.Application.Predictive.Jobs;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Application.Situations.Jobs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.AI;
using EmergencyResponse.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EmergencyResponse.UnitTests.AI;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class IncidentAIJobProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenAnalysisSucceeds_UpdatesIncidentAndSchedulesDownstreamWork()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var incident = await AddIncidentAsync(fixture);
        var dependencies = CreateDependencies();
        var aiResult = new IncidentAnalysisResult
        {
            IncidentType = "Fire",
            SeverityCandidate = "Critical",
            Confidence = 0.93
        };

        dependencies.Analysis
            .Setup(service => service.AnalyzeAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);
        dependencies.Decision
            .Setup(service => service.Evaluate(aiResult))
            .Returns(new IncidentDecisionResult
            {
                Severity = new SeverityEvaluationResult
                {
                    Score = 90,
                    Severity = SeverityLevel.Critical,
                    Reasons = ["Immediate threat"]
                },
                Priority = new PriorityEvaluationResult
                {
                    Score = 85,
                    Priority = PriorityLevel.Critical,
                    Reasons = ["Multiple people affected"]
                }
            });
        dependencies.ReviewPolicy
            .Setup(policy => policy.RequiresHumanReview(aiResult))
            .Returns(false);

        await dependencies.CreateProcessor(fixture).ProcessAsync(new IncidentAIJob(incident.Id));

        var savedIncident = await fixture.Context.Incidents.FindAsync(incident.Id);
        var savedAnalysis = Assert.Single(fixture.Context.AIAnalyses);
        Assert.NotNull(savedIncident);
        Assert.Equal(IncidentStatus.Submitted, savedIncident.Status);
        Assert.Equal(SeverityLevel.Critical, savedIncident.Severity);
        Assert.Equal(PriorityLevel.Critical, savedIncident.Priority);
        Assert.Equal(90, savedIncident.SeverityScore);
        Assert.Equal(85, savedIncident.PriorityScore);
        Assert.Equal(AIAnalysisStatus.Completed, savedAnalysis.Status);
        Assert.Equal(0.93, savedAnalysis.Confidence);
        Assert.Equal(1, savedAnalysis.AttemptNumber);
        Assert.Contains("Immediate threat", savedIncident.SeverityReasonsJson);

        dependencies.SituationQueue.Verify(queue => queue.EnqueueAsync(
            It.Is<SituationEvaluationJob>(job =>
                job.TriggerIncidentId == incident.Id &&
                job.TriggerReason == "AIAnalysisCompleted"),
            It.IsAny<CancellationToken>()), Times.Once);
        dependencies.PredictiveQueue.Verify(queue => queue.EnqueueAsync(
            It.Is<PredictiveAnalysisJob>(job =>
                job.IncidentId == incident.Id &&
                job.TriggerReason == "AIAnalysisCompleted"),
            It.IsAny<CancellationToken>()), Times.Once);
        dependencies.Notification.Verify(service => service.NotifyDashboardUpdatedAsync(
            It.Is<DashboardUpdatedEvent>(item =>
                item.IncidentId == incident.Id &&
                item.Reason == DashboardUpdateReasons.AIAnalysisCompleted),
            It.IsAny<CancellationToken>()), Times.Once);
        dependencies.Escalation.Verify(service => service.EvaluateIncidentAsync(
            incident.Id,
            IncidentSeverityBeforeProcessing,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenAICircuitIsOpen_PersistsRetryableFailureWithoutChangingIncidentSeverity()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var incident = await AddIncidentAsync(fixture);
        var originalSeverity = incident.Severity;
        var originalPriority = incident.Priority;
        var dependencies = CreateDependencies();
        var circuitOpen = new AIServiceCircuitOpenException("AI circuit is open.");

        dependencies.Analysis
            .Setup(service => service.AnalyzeAsync(
                It.IsAny<IncidentAnalysisInput>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(circuitOpen);

        await dependencies.CreateProcessor(fixture).ProcessAsync(new IncidentAIJob(incident.Id));

        var savedIncident = await fixture.Context.Incidents.FindAsync(incident.Id);
        var savedAnalysis = Assert.Single(fixture.Context.AIAnalyses);
        Assert.NotNull(savedIncident);
        Assert.Equal(IncidentStatus.Submitted, savedIncident.Status);
        Assert.Equal(originalSeverity, savedIncident.Severity);
        Assert.Equal(originalPriority, savedIncident.Priority);
        Assert.Equal(AIAnalysisStatus.Failed, savedAnalysis.Status);
        Assert.Equal("DependencyUnavailable", savedAnalysis.FailureCategory);
        Assert.Equal(circuitOpen.Message, savedAnalysis.FailureReason);
        Assert.Equal(1, savedAnalysis.RetryCount);
        Assert.NotNull(savedAnalysis.NextRetryAtUtc);
        Assert.True(savedAnalysis.NextRetryAtUtc > DateTime.UtcNow);

        dependencies.SituationQueue.Verify(queue => queue.EnqueueAsync(
            It.IsAny<SituationEvaluationJob>(),
            It.IsAny<CancellationToken>()), Times.Never);
        dependencies.PredictiveQueue.Verify(queue => queue.EnqueueAsync(
            It.IsAny<PredictiveAnalysisJob>(),
            It.IsAny<CancellationToken>()), Times.Never);
        dependencies.Decision.Verify(service => service.Evaluate(It.IsAny<IncidentAnalysisResult>()), Times.Never);
    }

    private static SeverityLevel IncidentSeverityBeforeProcessing => SeverityLevel.Moderate;

    private static async Task<Incident> AddIncidentAsync(SqliteApplicationDbFixture fixture)
    {
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Fire",
            Code = $"FIRE-{Guid.NewGuid():N}"[..12],
            IsActive = true
        };
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = $"INC-{Guid.NewGuid():N}",
            Title = "Warehouse fire",
            Description = "Smoke and flame reported.",
            IncidentType = incidentType,
            IncidentTypeId = incidentType.Id,
            Status = IncidentStatus.Submitted,
            Severity = IncidentSeverityBeforeProcessing,
            Priority = PriorityLevel.Normal,
            CreatedAtUtc = DateTime.UtcNow,
            ReportedAtUtc = DateTime.UtcNow
        };
        incident.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Latitude = 28.61m,
            Longitude = 77.20m,
            Address = "Test Street",
            City = "New Delhi",
            IsPrimary = true
        });

        fixture.Context.Incidents.Add(incident);
        await fixture.Context.SaveChangesAsync();
        return incident;
    }

    private static TestDependencies CreateDependencies()
    {
        var analysis = new Mock<IIncidentAnalysisService>(MockBehavior.Strict);
        var decision = new Mock<IIncidentDecisionService>(MockBehavior.Strict);
        var reviewPolicy = new Mock<IAIReviewPolicy>(MockBehavior.Strict);
        var duplicateService = new Mock<IDuplicateIncidentService>(MockBehavior.Strict);
        duplicateService
            .Setup(service => service.GenerateEmbeddingAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        duplicateService
            .Setup(service => service.FindCandidatesAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DuplicateIncidentCandidate>());

        var notification = new Mock<IIncidentNotificationService>(MockBehavior.Strict);
        notification
            .Setup(service => service.NotifyDashboardUpdatedAsync(
                It.IsAny<DashboardUpdatedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notification
            .Setup(service => service.NotifyIncidentUpdatedAsync(
                It.IsAny<IncidentUpdatedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var escalation = new Mock<IIncidentEscalationService>(MockBehavior.Strict);
        escalation
            .Setup(service => service.EvaluateIncidentAsync(
                It.IsAny<Guid>(),
                It.IsAny<SeverityLevel?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmergencyResponse.Application.Escalation.DTOs.IncidentEscalationResponse>());

        var situationQueue = new Mock<ISituationEvaluationQueue>(MockBehavior.Strict);
        situationQueue
            .Setup(queue => queue.EnqueueAsync(
                It.IsAny<SituationEvaluationJob>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var predictiveQueue = new Mock<IPredictiveAnalysisQueue>(MockBehavior.Strict);
        predictiveQueue
            .Setup(queue => queue.EnqueueAsync(
                It.IsAny<PredictiveAnalysisJob>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return new TestDependencies(
            analysis,
            decision,
            reviewPolicy,
            duplicateService,
            notification,
            escalation,
            situationQueue,
            predictiveQueue);
    }

    private sealed record TestDependencies(
        Mock<IIncidentAnalysisService> Analysis,
        Mock<IIncidentDecisionService> Decision,
        Mock<IAIReviewPolicy> ReviewPolicy,
        Mock<IDuplicateIncidentService> DuplicateService,
        Mock<IIncidentNotificationService> Notification,
        Mock<IIncidentEscalationService> Escalation,
        Mock<ISituationEvaluationQueue> SituationQueue,
        Mock<IPredictiveAnalysisQueue> PredictiveQueue)
    {
        public IncidentAIJobProcessor CreateProcessor(SqliteApplicationDbFixture fixture)
            => new(
                Mock.Of<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(),
                fixture.Context,
                Mock.Of<IIncidentAIOrchestrationService>(),
                Analysis.Object,
                Decision.Object,
                Options.Create(new AIOptions { Model = "test-model" }),
                NullLogger<IncidentAIJobProcessor>.Instance,
                ReviewPolicy.Object,
                Notification.Object,
                DuplicateService.Object,
                Escalation.Object,
                SituationQueue.Object,
                PredictiveQueue.Object);
    }
}

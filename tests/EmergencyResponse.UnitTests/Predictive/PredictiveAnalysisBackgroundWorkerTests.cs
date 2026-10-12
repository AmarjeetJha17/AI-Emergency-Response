using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Predictive;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Interfaces;
using EmergencyResponse.Application.Predictive.Jobs;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Predictive;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class PredictiveAnalysisBackgroundWorkerTests
{
    [Fact]
    public async Task Worker_WhenPredictionSucceeds_PublishesPredictionAndMarksWorkerHealthy()
    {
        var incidentId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var prediction = CreatePrediction(incidentId, generatedAt);
        var notificationReceived = new TaskCompletionSource<PredictionUpdatedEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var predictionService = new Mock<IEscalationRiskPredictionService>(MockBehavior.Strict);
        predictionService
            .Setup(service => service.PredictAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prediction);
        var notifications = CreateNotificationMock(notificationReceived);
        var dependencies = await WorkerDependencies.CreateAsync(
            predictionService,
            notifications,
            incidentId);

        await using (dependencies)
        {
            await dependencies.Queue.EnqueueAsync(new PredictiveAnalysisJob(incidentId, "UnitTest"));
            await dependencies.Worker.StartAsync(CancellationToken.None);
            try
            {
                var update = await notificationReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await WaitForWorkerSuccessAsync(dependencies.WorkerHealth);

                Assert.Equal(incidentId, update.IncidentId);
                Assert.Equal("EscalationRisk", update.PredictionType);
                Assert.True(update.PredictedEscalation);
                Assert.Equal(0.87, update.Probability);
                Assert.Equal("test-model", update.ModelName);
                Assert.Equal(generatedAt, update.GeneratedAtUtc);
                Assert.NotNull(dependencies.WorkerHealth.GetLastSuccessUtc(
                    nameof(PredictiveAnalysisBackgroundWorker)));
                predictionService.Verify(service => service.PredictAsync(
                    incidentId,
                    It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                await dependencies.Worker.StopAsync(CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task Worker_WhenCircuitIsOpen_LogsFailureAndContinuesWithNextJob()
    {
        var failedIncidentId = Guid.NewGuid();
        var recoveredIncidentId = Guid.NewGuid();
        var recoveredPrediction = CreatePrediction(recoveredIncidentId, DateTime.UtcNow);
        var notificationReceived = new TaskCompletionSource<PredictionUpdatedEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var predictionService = new Mock<IEscalationRiskPredictionService>(MockBehavior.Strict);
        predictionService
            .SetupSequence(service => service.PredictAsync(
                failedIncidentId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIServiceCircuitOpenException("AI circuit is open."));
        predictionService
            .SetupSequence(service => service.PredictAsync(
                recoveredIncidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recoveredPrediction);
        var notifications = CreateNotificationMock(notificationReceived);
        var dependencies = await WorkerDependencies.CreateAsync(
            predictionService,
            notifications,
            failedIncidentId,
            recoveredIncidentId);

        await using (dependencies)
        {
            await dependencies.Queue.EnqueueAsync(new PredictiveAnalysisJob(failedIncidentId, "CircuitOpen"));
            await dependencies.Queue.EnqueueAsync(new PredictiveAnalysisJob(recoveredIncidentId, "Recovery"));
            await dependencies.Worker.StartAsync(CancellationToken.None);
            try
            {
                var update = await notificationReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await WaitForWorkerSuccessAsync(dependencies.WorkerHealth);

                Assert.Equal(recoveredIncidentId, update.IncidentId);
                Assert.NotNull(dependencies.WorkerHealth.GetLastSuccessUtc(
                    nameof(PredictiveAnalysisBackgroundWorker)));
                predictionService.Verify(service => service.PredictAsync(
                    failedIncidentId,
                    It.IsAny<CancellationToken>()), Times.Once);
                predictionService.Verify(service => service.PredictAsync(
                    recoveredIncidentId,
                    It.IsAny<CancellationToken>()), Times.Once);
                notifications.Verify(service => service.NotifyPredictionUpdatedAsync(
                    It.Is<PredictionUpdatedEvent>(item => item.IncidentId == failedIncidentId),
                    It.IsAny<CancellationToken>()), Times.Never);
            }
            finally
            {
                await dependencies.Worker.StopAsync(CancellationToken.None);
            }
        }
    }

    private static Mock<IIncidentNotificationService> CreateNotificationMock(
        TaskCompletionSource<PredictionUpdatedEvent> notificationReceived)
    {
        var notifications = new Mock<IIncidentNotificationService>(MockBehavior.Strict);
        notifications
            .Setup(service => service.NotifyPredictionUpdatedAsync(
                It.IsAny<PredictionUpdatedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<PredictionUpdatedEvent, CancellationToken>(
                (update, _) => notificationReceived.TrySetResult(update));
        return notifications;
    }

    private static EscalationRiskPrediction CreatePrediction(Guid incidentId, DateTime generatedAt)
        => new()
        {
            IncidentId = incidentId,
            PredictedEscalation = true,
            Probability = 0.87,
            RiskScore = 0.91,
            ModelName = "test-model",
            ModelVersion = "test-v1",
            GeneratedAtUtc = generatedAt
        };

    private static async Task WaitForWorkerSuccessAsync(WorkerHealthState workerHealth)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (workerHealth.GetLastSuccessUtc(nameof(PredictiveAnalysisBackgroundWorker)) is null &&
               DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.NotNull(workerHealth.GetLastSuccessUtc(nameof(PredictiveAnalysisBackgroundWorker)));
    }

    private sealed class WorkerDependencies : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly SqliteConnection _connection;

        private WorkerDependencies(
            ServiceProvider services,
            SqliteConnection connection,
            InMemoryPredictiveAnalysisQueue queue,
            PredictiveAnalysisBackgroundWorker worker,
            WorkerHealthState workerHealth)
        {
            _services = services;
            _connection = connection;
            Queue = queue;
            Worker = worker;
            WorkerHealth = workerHealth;
        }

        public InMemoryPredictiveAnalysisQueue Queue { get; }

        public PredictiveAnalysisBackgroundWorker Worker { get; }

        public WorkerHealthState WorkerHealth { get; }

        public static async Task<WorkerDependencies> CreateAsync(
            Mock<IEscalationRiskPredictionService> predictionService,
            Mock<IIncidentNotificationService> notifications,
            params Guid[] incidentIds)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var features = new Mock<IEscalationPredictionFeatureBuilder>(MockBehavior.Strict);
            foreach (var incidentId in incidentIds)
            {
                var featureSet = new EscalationPredictionFeatures { SeverityScore = 0.75f };
                features
                    .Setup(builder => builder.BuildSnapshotAsync(
                        incidentId,
                        It.IsAny<DateTime>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PredictionFeatureSnapshot
                    {
                        IncidentId = incidentId,
                        SnapshotAtUtc = DateTime.UtcNow,
                        FeatureHash = PredictionFeatureSnapshot.ComputeFeatureHash(featureSet),
                        Features = featureSet
                    });
            }

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(builder => builder.UseSqlite(connection));
            services.AddSingleton(features.Object);
            services.AddSingleton(predictionService.Object);
            services.AddSingleton(notifications.Object);
            var provider = services.BuildServiceProvider();
            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                    .Database.EnsureCreatedAsync();
            }

            var queue = new InMemoryPredictiveAnalysisQueue();
            var workerHealth = new WorkerHealthState();
            var worker = new PredictiveAnalysisBackgroundWorker(
                queue,
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<PredictiveAnalysisBackgroundWorker>.Instance,
                workerHealth);

            return new WorkerDependencies(provider, connection, queue, worker, workerHealth);
        }

        public async ValueTask DisposeAsync()
        {
            await _services.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

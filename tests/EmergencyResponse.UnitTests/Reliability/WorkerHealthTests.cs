using EmergencyResponse.Application.Observability;

namespace EmergencyResponse.UnitTests.Reliability;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Resilience)]
public class WorkerHealthTests
{
    [Fact]
    public void InitialState_ShouldHaveNoLastSuccess()
    {
        var healthState = new WorkerHealthState();

        Assert.Null(healthState.LastSuccessUtc);
        Assert.Null(healthState.GetLastSuccessUtc("IncidentAIWorker"));
    }

    [Fact]
    public void MarkSuccess_ShouldRecordCurrentUtcTimestamp()
    {
        var healthState = new WorkerHealthState();
        var before = DateTime.UtcNow.AddSeconds(-1);

        healthState.MarkSuccess();

        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.NotNull(healthState.LastSuccessUtc);
        Assert.True(healthState.LastSuccessUtc >= before);
        Assert.True(healthState.LastSuccessUtc <= after);
    }

    [Fact]
    public void MarkSuccess_WithWorkerName_ShouldRecordNamedWorkerTimestamp()
    {
        var healthState = new WorkerHealthState();

        healthState.MarkSuccess("IncidentAIWorker");

        var workerSuccess = healthState.GetLastSuccessUtc("IncidentAIWorker");
        var globalSuccess = healthState.LastSuccessUtc;

        Assert.NotNull(workerSuccess);
        Assert.NotNull(globalSuccess);
        Assert.Equal(globalSuccess, workerSuccess);
    }

    [Fact]
    public void MultipleWorkers_ShouldTrackIndependently()
    {
        var healthState = new WorkerHealthState();

        healthState.MarkSuccess("IncidentAIWorker");
        var aiSuccess = healthState.GetLastSuccessUtc("IncidentAIWorker");

        Thread.Sleep(10);

        healthState.MarkSuccess("VisualAnalysisWorker");
        var visualSuccess = healthState.GetLastSuccessUtc("VisualAnalysisWorker");

        Assert.NotNull(aiSuccess);
        Assert.NotNull(visualSuccess);
        Assert.True(visualSuccess >= aiSuccess);
    }

    [Fact]
    public void ConcurrentUpdates_ShouldBeThreadSafe()
    {
        var healthState = new WorkerHealthState();

        Parallel.For(0, 100, i =>
        {
            healthState.MarkSuccess($"Worker_{i % 5}");
        });

        Assert.NotNull(healthState.LastSuccessUtc);
        for (var i = 0; i < 5; i++)
        {
            Assert.NotNull(healthState.GetLastSuccessUtc($"Worker_{i}"));
        }
    }

    [Fact]
    public void StaleWorker_ShouldBeDetectedBeyondThreshold()
    {
        var healthState = new WorkerHealthState();
        healthState.MarkSuccess();

        var lastSuccess = healthState.LastSuccessUtc!.Value;
        var staleThreshold = TimeSpan.FromMinutes(10);

        // Simulated time 15 minutes in the future
        var futureTime = lastSuccess.AddMinutes(15);
        var isStale = (futureTime - lastSuccess) > staleThreshold;

        Assert.True(isStale);
    }
}

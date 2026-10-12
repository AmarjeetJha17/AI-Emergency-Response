using EmergencyResponse.Application.Situations.Jobs;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.IntegrationTests.Situations;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection(IntegrationTestCollection.Name)]
public sealed class SituationEvaluationJobIntegrationTests : IntegrationTestBase
{
    public SituationEvaluationJobIntegrationTests(IntegrationTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void SituationEvaluationJob_EmptyConstructor_LeavesTriggerMetadataUnset()
    {
        var job = new SituationEvaluationJob();

        Assert.Null(job.TriggerIncidentId);
        Assert.Null(job.TriggerReason);
    }

    [Fact]
    public async Task EnqueuedEvaluationJob_CreatesUpdatesAndAcknowledgesSituation()
    {
        var incidentIds = await CreateIncidentClusterAsync(3);
        var triggerReason = $"IntegrationTest-{Guid.NewGuid():N}";
        var job = new SituationEvaluationJob(incidentIds[0], triggerReason);

        Assert.Equal(incidentIds[0], job.TriggerIncidentId);
        Assert.Equal(triggerReason, job.TriggerReason);

        var queue = Factory.Services.GetRequiredService<ISituationEvaluationQueue>();
        await queue.EnqueueAsync(job);

        var situation = await WaitForSituationAsync(incidentIds[0], expectedIncidentCount: 3);

        Assert.Equal(EscalationStatus.Open, situation.Status);
        Assert.True(situation.RequiresHumanReview);
        Assert.Equal(3, situation.IncidentCount);
        Assert.Equal(3, situation.Incidents.Count);
        Assert.All(incidentIds, incidentId =>
            Assert.Contains(situation.Incidents, membership => membership.IncidentId == incidentId));
        Assert.Single(situation.Incidents, membership => membership.IsPrimaryIncident);
        Assert.NotNull(situation.LastEvaluatedAtUtc);
        Assert.NotNull(situation.LastAIAnalysisAtUtc);
        Assert.NotEmpty(situation.Analyses);
        Assert.Equal("Test Operational Situation", situation.Title);

        var previousEvaluation = situation.LastEvaluatedAtUtc;
        var previousAnalysisCount = situation.Analyses.Count;
        await Task.Delay(TimeSpan.FromMilliseconds(25));

        var additionalIncidentId = await AddIncidentToClusterAsync(incidentIds.Count, casualties: 5);
        incidentIds.Add(additionalIncidentId);
        await queue.EnqueueAsync(new SituationEvaluationJob(
            additionalIncidentId,
            "AdditionalIncidentReported"));

        situation = await WaitForSituationAsync(
            incidentIds[0],
            expectedIncidentCount: 4,
            expectedSituationId: situation.Id);

        Assert.Equal(EscalationStatus.Open, situation.Status);
        Assert.Equal(4, situation.IncidentCount);
        Assert.Equal(4, situation.Incidents.Count);
        Assert.All(incidentIds, incidentId =>
            Assert.Contains(situation.Incidents, membership => membership.IncidentId == incidentId));
        Assert.True(situation.LastEvaluatedAtUtc > previousEvaluation);
        Assert.True(situation.Casualties >= 5);
        Assert.True(situation.Analyses.Count > previousAnalysisCount);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var situationService = scope.ServiceProvider.GetRequiredService<ISituationService>();
            await situationService.AcknowledgeAsync(
                situation.Id,
                "Acknowledged by the integration test operator.");
        }

        var acknowledgedSituation = await ReadSituationAsync(situation.Id);
        Assert.Equal(EscalationStatus.Acknowledged, acknowledgedSituation.Status);
        Assert.False(acknowledgedSituation.RequiresHumanReview);
    }

    private async Task<List<Guid>> CreateIncidentClusterAsync(int count)
    {
        var incidentTypeId = await ExecuteDbAsync(db => db.IncidentTypes
            .Select(type => type.Id)
            .FirstAsync());
        var incidents = Enumerable.Range(0, count)
            .Select(index => CreateClusterIncident(incidentTypeId, index, casualties: 0))
            .ToList();

        await ExecuteDbAsync(async db =>
        {
            db.Incidents.AddRange(incidents);
            await db.SaveChangesAsync();
        });

        return incidents.Select(incident => incident.Id).ToList();
    }

    private async Task<Guid> AddIncidentToClusterAsync(int index, int casualties)
    {
        var incidentTypeId = await ExecuteDbAsync(db => db.IncidentTypes
            .Select(type => type.Id)
            .FirstAsync());
        var incident = CreateClusterIncident(incidentTypeId, index, casualties);

        await ExecuteDbAsync(async db =>
        {
            db.Incidents.Add(incident);
            await db.SaveChangesAsync();
        });

        return incident.Id;
    }

    private static Incident CreateClusterIncident(Guid incidentTypeId, int index, int casualties)
    {
        var incident = TestDataFactory.CreateIncident(
            incidentTypeId,
            title: $"Situation evaluation job incident {Guid.NewGuid():N}",
            incidentNumber: $"JOB-{Guid.NewGuid():N}");

        incident.CreatedAtUtc = DateTime.UtcNow;
        incident.ReportedAtUtc = incident.CreatedAtUtc;
        incident.Severity = SeverityLevel.High;
        incident.Priority = PriorityLevel.High;
        incident.PeopleAffected = 2 + index;
        incident.Casualties = casualties;

        var location = TestDataFactory.CreateIncidentLocation(incident.Id);
        location.Latitude = 40.7128m + (index * 0.0001m);
        location.Longitude = -74.0060m + (index * 0.0001m);
        location.City = "Situation Evaluation Test Cluster";
        location.Address = $"Test location {index + 1}";
        incident.Locations.Add(location);

        return incident;
    }

    private async Task<OperationalSituation> WaitForSituationAsync(
        Guid triggerIncidentId,
        int expectedIncidentCount,
        Guid? expectedSituationId = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            var situation = await ExecuteDbAsync(async db =>
            {
                var query = db.OperationalSituations
                    .AsNoTracking()
                    .Include(item => item.Incidents)
                    .Include(item => item.Analyses)
                    .Where(item => item.Incidents.Any(
                        membership => membership.IncidentId == triggerIncidentId));

                if (expectedSituationId.HasValue)
                {
                    query = query.Where(item => item.Id == expectedSituationId.Value);
                }

                return await query.OrderByDescending(item => item.CreatedAtUtc).FirstOrDefaultAsync();
            });

            if (situation?.IncidentCount == expectedIncidentCount &&
                situation.Incidents.Count == expectedIncidentCount)
            {
                return situation;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException(
            $"The situation evaluation worker did not persist a situation with {expectedIncidentCount} incidents within 30 seconds.");
    }

    private async Task<OperationalSituation> ReadSituationAsync(Guid situationId)
        => await ExecuteDbAsync(async db => await db.OperationalSituations
            .AsNoTracking()
            .Include(item => item.Incidents)
            .Include(item => item.Analyses)
            .SingleAsync(item => item.Id == situationId));
}

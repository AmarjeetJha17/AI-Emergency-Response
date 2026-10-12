using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.LoadTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class RateLimitScenario
{
    public static async Task<object> RunAsync(LoadTestHostFactory host, LoadScenarioContext test, CancellationToken cancellationToken)
    {
        var prefix = $"RATE-LIMIT-{Guid.NewGuid():N}-";
        var successes = 0;
        var rejected = 0;
        var unexpected = new List<int>();
        for (var index = 0; index < 12; index++)
        {
            using var response = await test.Client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
            {
                Title = prefix + index,
                Description = "Synthetic request used to validate the configured incident-create rate limiter.",
                IncidentTypeId = test.Data.IncidentTypeId,
                Latitude = 18.5204m,
                Longitude = 73.8567m,
                City = "Load Test City"
            }, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Created) successes++;
            else if (response.StatusCode == HttpStatusCode.TooManyRequests) rejected++;
            else unexpected.Add((int)response.StatusCode);
        }

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await db.Incidents.AsNoTracking().CountAsync(incident => incident.Title.StartsWith(prefix), cancellationToken);
        var passed = successes == 10 && rejected == 2 && unexpected.Count == 0 && persisted == successes;
        return new
        {
            scenario = "incident-create-rate-limit",
            configuredPermitsPerMinute = 10,
            requests = 12,
            created = successes,
            rejected429 = rejected,
            unexpectedStatuses = unexpected,
            persistedSideEffects = persisted,
            rejectedRequestsCreatedNothing = persisted == successes,
            passed
        };
    }
}

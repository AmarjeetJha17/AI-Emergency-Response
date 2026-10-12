using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.LoadTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class ChaosScenario
{
    public static async Task<object> RunAsync(LoadScenarioContext test, IServiceProvider services, CancellationToken cancellationToken)
    {
        var results = new List<ChaosCheck>();

        test.Faults.AIUnavailable = true;
        try
        {
            var created = await test.SendForInspectionAsync(HttpMethod.Post, "/api/incidents", CreateRequest(test), cancellationToken);
            var createdId = TryGetCreatedId(created.Body);
            var readable = created.StatusCode == HttpStatusCode.Created && createdId.HasValue &&
                           (await test.SendForInspectionAsync(HttpMethod.Get, $"/api/incidents/{createdId}", cancellationToken: cancellationToken)).StatusCode == HttpStatusCode.OK;
            results.Add(new ChaosCheck("ai-unavailable", readable,
                "Incident creation and detail retrieval succeed while the deterministic AI provider fake is unavailable."));
        }
        finally
        {
            test.Faults.AIUnavailable = false;
        }

        test.Faults.PublishUnavailable = true;
        try
        {
            var created = await test.SendForInspectionAsync(HttpMethod.Post, "/api/incidents", CreateRequest(test), cancellationToken);
            var createdId = TryGetCreatedId(created.Body);
            var retainedJobs = 0;
            if (createdId.HasValue)
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                retainedJobs = await db.OutboxMessages.AsNoTracking()
                    .CountAsync(message => !message.IsPublished && message.PayloadJson.Contains(createdId.Value.ToString()), cancellationToken);
            }
            results.Add(new ChaosCheck("publisher-unavailable", created.StatusCode == HttpStatusCode.Created && retainedJobs > 0,
                $"Incident creation succeeds and {retainedJobs} outbox jobs remain pending while publication is unavailable."));
        }
        finally
        {
            test.Faults.PublishUnavailable = false;
        }

        test.Faults.SearchUnavailable = true;
        try
        {
            var result = await test.Client.PostAsJsonAsync("/api/copilot/query",
                new CopilotQuery { Question = "Search emergency procedures for synthetic test guidance." }, cancellationToken);
            var response = await result.Content.ReadFromJsonAsync<CopilotResponse>(cancellationToken: cancellationToken);
            var safe = result.IsSuccessStatusCode && response is not null && !response.SourcesSufficient &&
                       response.Citations.Count == 0 && response.RequiresHumanVerification;
            results.Add(new ChaosCheck("search-unavailable", safe,
                safe ? "Copilot returned an ungrounded, human-verification response without citations." : "Response did not meet the no-grounding contract."));
        }
        finally
        {
            test.Faults.SearchUnavailable = false;
        }

        test.Faults.WeatherUnavailable = true;
        try
        {
            var created = await test.SendForInspectionAsync(HttpMethod.Post, "/api/incidents", CreateRequest(test), cancellationToken);
            var createdId = TryGetCreatedId(created.Body);
            var detailWorks = createdId.HasValue && (await test.SendForInspectionAsync(HttpMethod.Get,
                $"/api/incidents/{createdId}", cancellationToken: cancellationToken)).StatusCode == HttpStatusCode.OK;
            var environment = createdId.HasValue
                ? await test.SendForInspectionAsync(HttpMethod.Get, $"/api/incidents/{createdId}/environment", cancellationToken: cancellationToken)
                : default;
            var noFakeWeather = environment.StatusCode == HttpStatusCode.NotFound ||
                                (int)environment.StatusCode is >= 200 and < 300 && !environment.Body.Contains("Synthetic test context", StringComparison.OrdinalIgnoreCase);
            results.Add(new ChaosCheck("weather-unavailable", created.StatusCode == HttpStatusCode.Created && detailWorks && noFakeWeather,
                "Incident reads remain available; unavailable weather is not replaced with synthetic provider data."));
        }
        finally
        {
            test.Faults.WeatherUnavailable = false;
        }

        foreach (var delay in new[] { 100, 250, 500, 1000 })
        {
            test.Faults.DatabaseDelayMs = delay;
            var started = DateTime.UtcNow;
            var response = await test.SendForInspectionAsync(HttpMethod.Get, "/api/dashboard/summary", cancellationToken: cancellationToken);
            var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
            results.Add(new ChaosCheck($"sql-delay-{delay}ms", response.StatusCode == HttpStatusCode.OK,
                $"Dashboard status {(int)response.StatusCode}; observed request time {Math.Round(elapsed, 1)} ms."));
        }

        test.Faults.DatabaseDelayMs = 0;
        test.Faults.DatabaseFailure = true;
        try
        {
            var failed = await test.SendForInspectionAsync(HttpMethod.Get, "/api/dashboard/summary", cancellationToken: cancellationToken);
            results.Add(new ChaosCheck("sql-failure", failed.StatusCode == HttpStatusCode.InternalServerError,
                $"Injected SQL failure returned HTTP {(int)failed.StatusCode}."));
        }
        finally
        {
            test.Faults.DatabaseFailure = false;
        }

        var recovered = await test.SendForInspectionAsync(HttpMethod.Get, "/api/dashboard/summary", cancellationToken: cancellationToken);
        results.Add(new ChaosCheck("sql-recovery", recovered.StatusCode == HttpStatusCode.OK,
            $"Dashboard returned HTTP {(int)recovered.StatusCode} after the SQL fault was removed."));

        return new
        {
            scenario = "deterministic-dependency-chaos",
            environment = "LocalTestHost only",
            checks = results,
            passed = results.Count(item => item.Passed),
            failed = results.Count(item => !item.Passed)
        };
    }

    private static CreateIncidentRequest CreateRequest(LoadScenarioContext test) => new()
    {
        Title = $"Chaos fixture {Guid.NewGuid():N}",
        Description = "Synthetic chaos-test incident; no real provider calls are enabled.",
        IncidentTypeId = test.Data.IncidentTypeId,
        Latitude = 18.5204m,
        Longitude = 73.8567m,
        City = "Load Test City"
    };

    private static Guid? TryGetCreatedId(string body)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("id", out var id) && id.TryGetGuid(out var value) ? value : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record ChaosCheck(string Name, bool Passed, string Detail);
}

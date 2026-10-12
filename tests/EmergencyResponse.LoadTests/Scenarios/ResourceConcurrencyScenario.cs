using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class ResourceConcurrencyScenario
{
    public static ScenarioProps Create(LoadScenarioContext test)
    {
        var resourceId = test.Data.ResourceIds.FirstOrDefault();
        if (resourceId == Guid.Empty) throw new InvalidOperationException("The selected dataset has no resource fixture.");

        return Scenario.Create("resource-concurrent-operators", async context =>
        {
            var incidentId = test.NextIncidentId();
            var assignmentTimer = Stopwatch.StartNew();
            using var assign = await test.Client.PostAsJsonAsync($"/api/resources/{resourceId}/assign",
                new AssignResourceRequest { IncidentId = incidentId, Notes = "Synthetic concurrency test." },
                context.ScenarioCancellationToken);
            var assignmentAccepted = assign.StatusCode == HttpStatusCode.OK;
            test.Metrics.Record(assignmentAccepted ? "resource.assign" : "resource.assign-contended",
                assignmentTimer.Elapsed, assignmentAccepted || assign.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);

            if (!assignmentAccepted)
            {
                return assign.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict
                    ? Response.Ok(statusCode: ((int)assign.StatusCode).ToString(), message: "Concurrent assignment was rejected; state remains protected.")
                    : Response.Fail(statusCode: ((int)assign.StatusCode).ToString(), message: "Unexpected resource assignment response");
            }

            try
            {
                var commands = Enumerable.Range(0, 4).Select(async _ =>
                {
                    var timer = Stopwatch.StartNew();
                    using var response = await test.Client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
                        new ResourceCommandRequest { Command = ResourceCommand.Dispatch, Notes = "Concurrent synthetic dispatch." },
                        context.ScenarioCancellationToken);
                    var expected = response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.Conflict;
                    var metric = response.StatusCode == HttpStatusCode.Conflict ? "resource.command-409" :
                        response.StatusCode == HttpStatusCode.BadRequest ? "resource.command-400" : "resource.command-200";
                    test.Metrics.Record(metric, timer.Elapsed, expected);
                    return expected;
                });

                var outcomes = await Task.WhenAll(commands);
                return outcomes.All(value => value)
                    ? Response.Ok()
                    : Response.Fail(message: "Unexpected resource command response under concurrency");
            }
            finally
            {
                using var release = await test.Client.PostAsJsonAsync($"/api/resources/{resourceId}/release",
                    new ReleaseResourceRequest { Notes = "Concurrency test cleanup." }, context.ScenarioCancellationToken);
                if (release.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.BadRequest or HttpStatusCode.NotFound))
                {
                    test.Metrics.Record("resource.release-cleanup", TimeSpan.Zero, false);
                }
            }
        });
    }
}

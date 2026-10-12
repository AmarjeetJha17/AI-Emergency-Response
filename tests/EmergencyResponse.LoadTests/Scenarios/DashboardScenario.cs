using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class DashboardScenario
{
    private static readonly string[] Endpoints =
    [
        "/api/dashboard/summary",
        "/api/dashboard/incidents?take=50",
        "/api/dashboard/severity",
        "/api/dashboard/trend?days=30"
    ];

    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("dashboard", async context =>
    {
        var index = context.Random.Next(Endpoints.Length);
        var path = Endpoints[index];
        var result = await test.RequestAsync($"dashboard.{EndpointName(path)}", HttpMethod.Get, path);
        if (result.IsError)
        {
            return result;
        }

        // Each request is one endpoint. The separate dashboard-combined scenario below
        // exercises all four calls in a single operator refresh cycle.
        return result;
    });

    public static ScenarioProps CreateCombined(LoadScenarioContext test) => Scenario.Create("dashboard-combined", async _ =>
    {
        foreach (var path in Endpoints)
        {
            var result = await test.RequestAsync($"dashboard.{EndpointName(path)}", HttpMethod.Get, path);
            if (result.IsError)
            {
                return result;
            }
        }

        return NBomber.CSharp.Response.Ok();
    });

    private static string EndpointName(string path) => path.Contains("summary", StringComparison.Ordinal)
        ? "summary"
        : path.Contains("incidents", StringComparison.Ordinal)
            ? "incidents"
            : path.Contains("severity", StringComparison.Ordinal) ? "severity" : "trend";
}

using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class MixedOperationsScenario
{
    private static readonly int[] Distribution = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 3, 3, 4, 5, 6, 7];

    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("mixed-operations", async context =>
    {
        return Distribution[context.Random.Next(Distribution.Length)] switch
        {
            0 => await test.RequestAsync("incident.list", HttpMethod.Get, "/api/incidents?page=1&pageSize=25"),
            1 => await test.RequestAsync("dashboard.summary", HttpMethod.Get, "/api/dashboard/summary"),
            2 => await test.RequestAsync("map.incidents", HttpMethod.Get, "/api/map/incidents"),
            3 => await test.RequestAsync("incident.detail", HttpMethod.Get, $"/api/incidents/{test.NextIncidentId()}"),
            4 => await test.RequestAsync("notification.summary", HttpMethod.Get, "/api/notifications/summary"),
            5 => await test.RequestAsync("resource.command-center", HttpMethod.Get, "/api/resources/command-center"),
            6 => await test.RequestAsync("situation.command-center", HttpMethod.Get, "/api/situations/command-center"),
            _ => await test.RequestAsync("dashboard.trend", HttpMethod.Get, "/api/dashboard/trend?days=30")
        };
    });
}

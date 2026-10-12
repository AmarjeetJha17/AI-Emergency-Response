using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class MapScenario
{
    private static readonly string[] Endpoints = ["/api/map/incidents", "/api/map/resources", "/api/situations/open/map"];

    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("map", async context =>
    {
        var index = context.Random.Next(Endpoints.Length);
        return await test.RequestAsync($"map.{index}", HttpMethod.Get, Endpoints[index]);
    });
}

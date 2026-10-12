using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class ResourceScenario
{
    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("resource-command-center", async context =>
    {
        var paths = new[]
        {
            "/api/resources",
            "/api/resources/command-center",
            "/api/resources/summary",
            $"/api/resources/{test.NextResourceId()}"
        };
        var path = paths[context.Random.Next(paths.Length)];
        return await test.RequestAsync("resource.read", HttpMethod.Get, path);
    });
}

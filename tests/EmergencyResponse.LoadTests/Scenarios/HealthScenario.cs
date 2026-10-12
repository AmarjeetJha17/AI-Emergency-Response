using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class HealthScenario
{
    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("health", async _ =>
        await test.RequestAsync("health.live", HttpMethod.Get, "/health/live"));
}

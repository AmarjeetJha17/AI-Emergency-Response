using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class SituationScenario
{
    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("situation-command-center", async _ =>
        await test.RequestAsync("situation.command-center", HttpMethod.Get, "/api/situations/command-center"));

    public static ScenarioProps CreateDetails(LoadScenarioContext test) => Scenario.Create("situation-detail", async _ =>
        await test.RequestAsync("situation.detail", HttpMethod.Get, $"/api/situations/{test.NextSituationId()}"));
}

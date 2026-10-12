using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class IncidentReadScenario
{
    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("incident-read", async _ =>
        await test.RequestAsync("incident.list", HttpMethod.Get, "/api/incidents?page=1&pageSize=25"));

    public static ScenarioProps CreateDetails(LoadScenarioContext test) => Scenario.Create("incident-detail", async _ =>
        await test.RequestAsync("incident.detail", HttpMethod.Get, $"/api/incidents/{test.NextIncidentId()}"));
}

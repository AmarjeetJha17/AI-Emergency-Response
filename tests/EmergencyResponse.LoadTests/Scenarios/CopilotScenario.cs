using EmergencyResponse.Application.Copilot;
using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class CopilotScenario
{
    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("copilot-mock", async _ =>
        await test.RequestAsync("copilot.mock", HttpMethod.Post, "/api/copilot/query", new CopilotQuery
        {
            Question = "What synthetic load-test context is available?"
        }));
}

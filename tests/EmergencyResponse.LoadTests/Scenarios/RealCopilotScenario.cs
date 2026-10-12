using EmergencyResponse.Application.Copilot;
using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class RealCopilotScenario
{
    public static ScenarioProps Create(LoadScenarioContext test)
    {
        if (!test.Options.RealProvidersEnabled || test.Options.RealAiMaxRequests is < 1 or > 25 ||
            test.Options.RealAiCostEstimateUsd is null or <= 0)
        {
            throw new InvalidOperationException(
                "Real AI calls require --real-ai true, --real-ai-max-requests 1..25, and a positive --real-ai-cost-estimate-usd entered after estimating provider cost.");
        }

        if (string.Equals(test.Options.HostMode, "LocalTestHost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real AI benchmarks require External mode against a non-production staging endpoint.");
        }

        var sent = 0;
        return Scenario.Create("copilot-real-ai-capped", async _ =>
        {
            if (Interlocked.Increment(ref sent) > test.Options.RealAiMaxRequests)
            {
                return Response.Ok(message: "Real provider request cap reached; no request sent.");
            }

            return await test.RequestAsync("copilot.real-ai", HttpMethod.Post, "/api/copilot/query",
                new CopilotQuery { Question = "Summarize available operational information and identify missing evidence." });
        });
    }
}

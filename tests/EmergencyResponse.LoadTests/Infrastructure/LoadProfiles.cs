using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Infrastructure;

public static class LoadProfiles
{
    public static ScenarioProps Apply(ScenarioProps scenario, string profile, int warmupSeconds,
        int incidentCreateRequestsPerSecond = 2, int copilotRequestsPerSecond = 1, int signalRConnectionCount = 50,
        int smokeDurationSeconds = 30)
    {
        var configured = profile.ToLowerInvariant() switch
        {
            "smoke" => scenario.WithLoadSimulations(
                Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(smokeDurationSeconds))),
            "baseline" => scenario.WithLoadSimulations(
                Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(5))),
            "stress" => scenario.WithLoadSimulations(
                Simulation.RampingConstant(copies: 25, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 50, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 100, during: TimeSpan.FromMinutes(2))),
            "spike" => scenario.WithLoadSimulations(
                Simulation.RampingConstant(copies: 10, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 100, during: TimeSpan.FromSeconds(30)),
                Simulation.RampingConstant(copies: 10, during: TimeSpan.FromMinutes(2))),
            "soak" => scenario.WithLoadSimulations(
                Simulation.KeepConstant(copies: 25, during: TimeSpan.FromSeconds(3600))),
            "signalr" => scenario.WithLoadSimulations(
                Simulation.KeepConstant(copies: signalRConnectionCount, during: TimeSpan.FromMinutes(5))),
            "media" => scenario.WithLoadSimulations(
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))),
            "burst" => scenario.WithLoadSimulations(
                Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
                Simulation.Inject(rate: 25, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))),
            "create" => scenario.WithLoadSimulations(
                Simulation.Inject(rate: incidentCreateRequestsPerSecond, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))),
            "copilot" => scenario.WithLoadSimulations(
                Simulation.Inject(rate: copilotRequestsPerSecond, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))),
            "stresslarge" => scenario.WithLoadSimulations(
                Simulation.RampingConstant(copies: 25, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 50, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 100, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 200, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingConstant(copies: 400, during: TimeSpan.FromMinutes(2))),
            _ => throw new InvalidOperationException(
                $"Unknown profile '{profile}'. Use Smoke, Baseline, Stress, StressLarge, Spike, Soak, SignalR, Media, Burst, Create, or Copilot.")
        };

        return warmupSeconds > 0
            ? configured.WithWarmUpDuration(TimeSpan.FromSeconds(warmupSeconds))
            : configured.WithoutWarmUp();
    }
}

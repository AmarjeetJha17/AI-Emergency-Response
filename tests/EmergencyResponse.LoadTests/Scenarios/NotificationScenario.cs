using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class NotificationScenario
{
    public static ScenarioProps Create(LoadScenarioContext test) => Scenario.Create("notifications", async context =>
    {
        return context.Random.Next(4) switch
        {
            0 => await test.RequestAsync("notification.list", HttpMethod.Get, "/api/notifications?take=100"),
            1 => await test.RequestAsync("notification.summary", HttpMethod.Get, "/api/notifications/summary"),
            2 => await test.RequestAsync("notification.read", HttpMethod.Post, $"/api/notifications/{test.NextNotificationId()}/read"),
            _ => await test.RequestAsync("notification.read-all", HttpMethod.Post, "/api/notifications/read-all")
        };
    });
}

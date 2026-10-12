using System.Net.Http.Json;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.LoadTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class SignalRScenario
{
    public static ScenarioProps CreateConnections(LoadScenarioContext test) => Scenario.Create("signalr-connection", async context =>
    {
        var started = DateTime.UtcNow;
        try
        {
            await using var connection = BuildConnection(test);
            await connection.StartAsync(context.ScenarioCancellationToken);
            test.Metrics.Record("signalr.connection", DateTime.UtcNow - started, true);
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Reconnect is followed by an authoritative REST snapshot. SignalR is a
            // change hint; application state remains in SQL.
            await connection.StopAsync(context.ScenarioCancellationToken);
            await connection.StartAsync(context.ScenarioCancellationToken);
            var snapshot = await test.Client.GetAsync("/api/dashboard/summary", context.ScenarioCancellationToken);
            var succeeded = snapshot.IsSuccessStatusCode;
            test.Metrics.Record("signalr.connection-duration", DateTime.UtcNow - started, succeeded);
            test.Metrics.Record("signalr.reconnect-snapshot", DateTime.UtcNow - started, succeeded);
            return succeeded ? Response.Ok() : Response.Fail(message: $"Snapshot returned {(int)snapshot.StatusCode}");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            test.Metrics.Record("signalr.connection", DateTime.UtcNow - started, false);
            return Response.Fail(message: exception.GetType().Name);
        }
    });

    public static ScenarioProps CreateDelivery(LoadScenarioContext test) => Scenario.Create("signalr-event-delivery", async context =>
    {
        var started = DateTime.UtcNow;
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await using var connection = BuildConnection(test);
            connection.On<object>("DashboardUpdated", _ => delivered.TrySetResult());
            await connection.StartAsync(context.ScenarioCancellationToken);
            var request = new CreateIncidentRequest
            {
                Title = $"SignalR load-test {Guid.NewGuid():N}",
                Description = "Synthetic event-delivery fixture.",
                IncidentTypeId = test.Data.IncidentTypeId,
                Latitude = 18.5204m,
                Longitude = 73.8567m,
                City = "Load Test City"
            };
            using var response = await test.Client.PostAsJsonAsync("/api/incidents", request, context.ScenarioCancellationToken);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                test.Metrics.Record("signalr.event-delivery", DateTime.UtcNow - started, false);
                return Response.Fail(statusCode: ((int)response.StatusCode).ToString(), message: "Incident event trigger rejected");
            }

            var eventDelivered = await Task.WhenAny(delivered.Task,
                Task.Delay(TimeSpan.FromSeconds(5), context.ScenarioCancellationToken)) == delivered.Task;
            test.Metrics.Record("signalr.event-delivery", DateTime.UtcNow - started, eventDelivered);
            return eventDelivered ? Response.Ok() : Response.Fail(message: "DashboardUpdated was not received within five seconds");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            test.Metrics.Record("signalr.event-delivery", DateTime.UtcNow - started, false);
            return Response.Fail(message: exception.GetType().Name);
        }
    });

    private static HubConnection BuildConnection(LoadScenarioContext test)
    {
        var url = new Uri(new Uri(test.Options.BaseUrl), "/hubs/incidents");
        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(test.AccessToken);
                options.Transports = HttpTransportType.LongPolling;
                if (test.SignalRHandlerFactory is not null)
                {
                    options.HttpMessageHandlerFactory = _ => test.SignalRHandlerFactory();
                }
            })
            .WithAutomaticReconnect()
            .Build();
    }
}

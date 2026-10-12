using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.LoadTests.Data;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Infrastructure;

public sealed class LoadScenarioContext(
    HttpClient client,
    LoadTestOptions options,
    LoadTestDatabaseSnapshot data,
    ScenarioMetrics metrics,
    LoadTestFaultController faults,
    string accessToken,
    Func<HttpMessageHandler>? signalRHandlerFactory = null)
{
    private long _sequence;

    public HttpClient Client { get; } = client;
    public LoadTestOptions Options { get; } = options;
    public LoadTestDatabaseSnapshot Data { get; } = data;
    public ScenarioMetrics Metrics { get; } = metrics;
    public LoadTestFaultController Faults { get; } = faults;
    public string AccessToken { get; } = accessToken;
    public Func<HttpMessageHandler>? SignalRHandlerFactory { get; } = signalRHandlerFactory;

    public Guid NextIncidentId() => Pick(Data.IncidentIds);
    public Guid NextResourceId() => Pick(Data.ResourceIds);
    public Guid NextSituationId() => Pick(Data.SituationIds);
    public Guid NextNotificationId() => Pick(Data.NotificationIds);

    public async Task<IResponse> RequestAsync(
        string metricName,
        HttpMethod method,
        string path,
        object? body = null,
        Func<HttpResponseMessage, bool>? accepted = null,
        CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        var success = false;
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await Client.SendAsync(request, cancellationToken);
            success = accepted?.Invoke(response) ?? response.IsSuccessStatusCode;
            Metrics.Record(metricName, timer.Elapsed, success);
            return success
                ? Response.Ok(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture))
                : Response.Fail(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture), message: $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            Metrics.Record(metricName, timer.Elapsed, false);
            return Response.Fail(message: exception.GetType().Name);
        }
    }

    public async Task<(HttpStatusCode StatusCode, string Body)> SendForInspectionAsync(
        HttpMethod method,
        string path,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await Client.SendAsync(request, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private Guid Pick(IReadOnlyList<Guid> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("The selected dataset has no seeded fixture IDs.");
        }

        var index = (int)((ulong)Interlocked.Increment(ref _sequence) % (uint)values.Count);
        return values[index];
    }
}

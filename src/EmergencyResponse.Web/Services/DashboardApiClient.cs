using System.Net;
using System.Net.Http.Json;

using EmergencyResponse.Application.Dashboard;
using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Application.Predictive.Hotspots;

namespace EmergencyResponse.Web.Services;

public sealed class DashboardApiClient : IDashboardApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardApiClient> _logger;

    public DashboardApiClient(
        HttpClient httpClient,
        ILogger<DashboardApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/dashboard/summary",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<DashboardSummary>(cancellationToken)
            ?? new DashboardSummary();
    }

    public Task<ForecastResult> GetIncidentVolumeAsync(CancellationToken cancellationToken = default) =>
        GetForecastPayloadAsync<ForecastResult>("api/predictive/incident-volume?horizon=24", cancellationToken);

    public async Task<IReadOnlyList<HotspotResult>> GetHotspotsAsync(CancellationToken cancellationToken = default) =>
        await GetForecastPayloadAsync<List<HotspotResult>>("api/predictive/hotspots?hours=24", cancellationToken);

    private async Task<T> GetForecastPayloadAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new System.Text.Json.JsonException("Forecast response was empty.");
    }

    public async Task<IReadOnlyList<DashboardIncidentItem>> GetIncidentsAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/dashboard/incidents?take={take}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<DashboardIncidentItem>>(
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<DashboardChartPoint>> GetSeverityAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/dashboard/severity",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<DashboardChartPoint>>(
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<DashboardTimeSeriesPoint>> GetTrendAsync(
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/dashboard/trend?days={days}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<DashboardTimeSeriesPoint>>(
                cancellationToken)
            ?? [];
    }
}

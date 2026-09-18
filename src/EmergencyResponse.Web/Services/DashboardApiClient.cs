using System.Net;
using System.Net.Http.Json;

using EmergencyResponse.Application.Dashboard;

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

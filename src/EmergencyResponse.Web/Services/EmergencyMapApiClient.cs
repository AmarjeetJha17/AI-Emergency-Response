using System.Net;
using System.Net.Http.Json;

using EmergencyResponse.Application.Map;

namespace EmergencyResponse.Web.Services;

public sealed class EmergencyMapApiClient : IEmergencyMapApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmergencyMapApiClient> _logger;

    public EmergencyMapApiClient(
        HttpClient httpClient,
        ILogger<EmergencyMapApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmergencyMapIncident>> GetIncidentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/map/incidents",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<EmergencyMapIncident>>(
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<string>> GetIncidentTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/map/incident-types",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<string>>(
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<string>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/map/departments",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<string>>(
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<MapResource>> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/map/resources",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<MapResource>>(
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<MapOperationalZone>> GetOperationalZonesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/map/zones",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<MapOperationalZone>>(
                cancellationToken)
            ?? [];
    }
}

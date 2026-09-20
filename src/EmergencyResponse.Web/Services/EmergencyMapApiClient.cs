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
}

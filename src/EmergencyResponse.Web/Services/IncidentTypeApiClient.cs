using System.Net.Http.Json;

using EmergencyResponse.Application.DTOs.IncidentTypes;

namespace EmergencyResponse.Web.Services;

public sealed class IncidentTypeApiClient
    : IIncidentTypeApiClient
{
    private readonly HttpClient _httpClient;

    public IncidentTypeApiClient(
        HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<IncidentTypeResponse>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        var result =
            await _httpClient.GetFromJsonAsync<
                IReadOnlyCollection<IncidentTypeResponse>>(
                "api/incident-types",
                cancellationToken);

        return result ?? Array.Empty<IncidentTypeResponse>();
    }
}
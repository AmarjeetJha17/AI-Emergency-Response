using System.Text.Json;
using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class EnvironmentCopilotTools
{
    private readonly ApplicationDbContext _db;
    private readonly CopilotToolAuthorization _authorization;

    public EnvironmentCopilotTools(
        ApplicationDbContext db,
        CopilotToolAuthorization authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<CopilotEnvironmentResult>?> GetIncidentEnvironmentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        CopilotToolValidation.RequiredId(incidentId, nameof(incidentId));
        await _authorization.RequireIncidentAccessAsync(
            incidentId,
            cancellationToken);

        var context = await _db.IncidentEnvironmentalContexts
            .AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderByDescending(x => x.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (context is null)
        {
            return CopilotToolResult<CopilotEnvironmentResult>.Unavailable(
                "DATA_UNAVAILABLE",
                "No environmental observation is currently available.");
        }

        return CopilotToolResult<CopilotEnvironmentResult>.Available(
            new CopilotEnvironmentResult
            {
                ObservedAtUtc = context.ObservedAtUtc,
                Provider = context.Provider,
                TemperatureC = context.TemperatureC,
                WindSpeedKph = context.WindSpeedKph,
                PrecipitationMm = context.PrecipitationMm,
                VisibilityKm = context.VisibilityKm,
                HasSevereWeatherAlert = context.HasSevereWeatherAlert,
                SevereWeatherTypes = ParseList(context.SevereWeatherAlertTypesJson),
                AirQualityIndex = context.AirQualityIndex,
                SignificantTrafficDisruption =
                    !string.IsNullOrWhiteSpace(context.TrafficIncidentsJson)
            });
    }

    private static IReadOnlyList<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

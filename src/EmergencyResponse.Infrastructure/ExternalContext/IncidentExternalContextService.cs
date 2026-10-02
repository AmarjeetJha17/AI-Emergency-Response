using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.ExternalContext;

public sealed class IncidentExternalContextService : IIncidentExternalContextService
{
    private readonly ApplicationDbContext _db;
    private readonly IWeatherContextClient _weatherClient;
    private readonly ILogger<IncidentExternalContextService> _logger;

    public IncidentExternalContextService(
        ApplicationDbContext db,
        IWeatherContextClient weatherClient,
        ILogger<IncidentExternalContextService> logger)
    {
        _db = db;
        _weatherClient = weatherClient;
        _logger = logger;
    }

    public async Task<IncidentEnvironmentalContext?> CaptureAsync(
        Guid incidentId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var weather = await _weatherClient.GetCurrentAsync(
            latitude,
            longitude,
            cancellationToken);
        var alerts = await _weatherClient.GetSevereAlertsAsync(
            latitude,
            longitude,
            cancellationToken);

        if (weather is null && alerts.Count == 0)
        {
            _logger.LogInformation(
                "No external environmental context was available for incident {IncidentId}.",
                incidentId);
            return null;
        }

        var observedAt = weather?.ObservedAtUtc ?? DateTime.UtcNow;
        var alertTypes = alerts
            .Select(x => x.Type)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
        var canonical = JsonSerializer.Serialize(new
        {
            provider = "AzureMaps",
            observedAtUtc = observedAt,
            latitude,
            longitude,
            weather,
            alertTypes
        });
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        var existing = await _db.IncidentEnvironmentalContexts
            .FirstOrDefaultAsync(
                x => x.IncidentId == incidentId && x.ContextHash == hash,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var context = new IncidentEnvironmentalContext
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            ObservedAtUtc = observedAt,
            Provider = "AzureMaps",
            ProviderVersion = "1.1",
            Latitude = latitude,
            Longitude = longitude,
            TemperatureC = weather?.TemperatureC,
            ApparentTemperatureC = weather?.ApparentTemperatureC,
            RelativeHumidity = weather?.RelativeHumidity,
            WindSpeedKph = weather?.WindSpeedKph,
            WindDirectionDegrees = weather?.WindDirectionDegrees,
            PrecipitationMm = weather?.PrecipitationMm,
            VisibilityKm = weather?.VisibilityKm,
            PressureHpa = weather?.PressureHpa,
            CloudCoverPercent = weather?.CloudCoverPercent,
            WeatherPhrase = weather?.WeatherPhrase,
            HasSevereWeatherAlert = alerts.Count > 0,
            SevereWeatherAlertTypesJson = JsonSerializer.Serialize(alertTypes),
            ContextHash = hash
        };

        _db.IncidentEnvironmentalContexts.Add(context);
        await _db.SaveChangesAsync(cancellationToken);
        return context;
    }
}

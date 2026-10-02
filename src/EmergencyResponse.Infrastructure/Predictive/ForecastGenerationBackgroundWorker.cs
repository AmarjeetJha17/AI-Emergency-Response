using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Application.Predictive.ResourceDemand;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EmergencyResponse.Application.Observability;

namespace EmergencyResponse.Infrastructure.Predictive;

public sealed class ForecastGenerationBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForecastGenerationBackgroundWorker> _logger;
    private readonly IWorkerHealthState? _workerHealthState;

    public ForecastGenerationBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ForecastGenerationBackgroundWorker> logger,
        IWorkerHealthState? workerHealthState = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerHealthState = workerHealthState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _workerHealthState?.MarkStarted(nameof(ForecastGenerationBackgroundWorker));
        await GenerateAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await GenerateAsync(stoppingToken);
        }
    }

    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var volumeService = scope.ServiceProvider.GetRequiredService<IForecastingService>();
            var demandService = scope.ServiceProvider.GetRequiredService<IResourceDemandForecastService>();
            var hotspots = scope.ServiceProvider.GetRequiredService<
                Application.Predictive.Hotspots.IHotspotService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var volume = await volumeService.ForecastAsync(
                ForecastType.IncidentVolume,
                "INCIDENT_VOLUME:ALL",
                ForecastGranularity.Hour,
                24,
                cancellationToken);
            await PersistAsync(db, volume, cancellationToken);

            if (volume.IsAvailable)
            {
                await notifications.CreateAsync(new NotificationRequest
                {
                    Type = NotificationType.ForecastGenerated,
                    Priority = NotificationPriority.Normal,
                    Title = "Predictive outlook refreshed",
                    Message = "Incident volume forecasts have been refreshed for the next 24 hours.",
                    TargetRole = ApplicationRoles.Operator,
                    DeduplicationKey = $"ForecastGenerated:{DateTime.UtcNow:yyyyMMddHH}"
                }, cancellationToken);
            }

            foreach (var resourceType in await db.Resources
                         .AsNoTracking()
                         .Select(x => x.Type)
                         .Distinct()
                         .ToListAsync(cancellationToken))
            {
                var demand = await demandService.ForecastAsync(
                    resourceType.ToString(),
                    24,
                    cancellationToken);
                if (!demand.PotentialShortage)
                {
                    continue;
                }

                await notifications.CreateAsync(new NotificationRequest
                {
                    Type = NotificationType.PotentialResourceShortageForecast,
                    Priority = NotificationPriority.Important,
                    Title = "Potential Resource Shortage",
                    Message = $"Forecasted {demand.ResourceType} demand may exceed available capacity. " +
                              $"Peak demand: {demand.PeakForecastDemand}; available: {demand.CurrentAvailable}.",
                    TargetRole = ApplicationRoles.Operator,
                    DeduplicationKey = $"PotentialForecastShortage:{demand.ResourceType}:{DateTime.UtcNow:yyyyMMddHH}"
                }, cancellationToken);
            }

            var hotspotResults = await hotspots.GetHotspotsAsync(24, cancellationToken);
            if (hotspotResults.Any(x => x.Trend == "Increasing"))
            {
                await notifications.CreateAsync(new NotificationRequest
                {
                    Type = NotificationType.HotspotTrendChanged,
                    Priority = NotificationPriority.Important,
                    Title = "Forecast Hotspot Alert",
                    Message = "Incident activity is increasing in one or more operational grid cells.",
                    TargetRole = ApplicationRoles.Operator,
                    DeduplicationKey = $"HotspotTrend:{DateTime.UtcNow:yyyyMMddHH}"
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forecast generation worker failed.");
        }
    }

    private static async Task PersistAsync(
        ApplicationDbContext db,
        ForecastResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsAvailable || result.Points.Count == 0)
        {
            return;
        }

        db.OperationalForecasts.Add(new OperationalForecast
        {
            Id = Guid.NewGuid(),
            ForecastType = result.ForecastType,
            Granularity = result.Granularity,
            SeriesKey = result.SeriesKey,
            ModelName = result.ModelName,
            ModelVersion = result.ModelVersion,
            FeatureSchemaVersion = "forecast-features-v1",
            ForecastGeneratedAtUtc = result.GeneratedAtUtc,
            ForecastStartAtUtc = result.Points[0].TimestampUtc,
            ForecastEndAtUtc = result.Points[^1].TimestampUtc,
            Horizon = result.Points.Count,
            ForecastJson = JsonSerializer.Serialize(result.Points)
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

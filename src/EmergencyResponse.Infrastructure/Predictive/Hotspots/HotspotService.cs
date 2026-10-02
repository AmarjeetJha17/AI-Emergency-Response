using EmergencyResponse.Application.Predictive.Hotspots;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Predictive.Hotspots;

public sealed class HotspotService : IHotspotService
{
    private readonly ApplicationDbContext _db;
    private readonly HotspotOptions _options;

    public HotspotService(
        ApplicationDbContext db,
        IOptions<HotspotOptions>? options = null)
    {
        _db = db;
        _options = options?.Value ?? new HotspotOptions();
    }

    public async Task<IReadOnlyList<HotspotResult>> GetHotspotsAsync(
        int hours,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var recentStart = now.AddHours(-Math.Clamp(hours, 1, 168));
        var historicalStart = now.AddHours(-_options.HistoricalHours);
        var incidents = await _db.Incidents
            .AsNoTracking()
            .Include(x => x.Locations)
            .Where(x => x.ReportedAtUtc >= historicalStart &&
                        x.ReportedAtUtc <= now)
            .ToListAsync(cancellationToken);

        var groups = incidents
            .SelectMany(incident => incident.Locations
                .Where(location => location.Latitude != 0 || location.Longitude != 0)
                .Select(location => new
                {
                    Incident = incident,
                    Cell = GetCell(location.Latitude, location.Longitude)
                }))
            .GroupBy(x => x.Cell.CellKey);

        var results = new List<HotspotResult>();
        foreach (var group in groups)
        {
            var rows = group.ToList();
            var recent = rows.Where(x => x.Incident.ReportedAtUtc >= recentStart).ToList();
            if (recent.Count < _options.MinimumActivity)
            {
                continue;
            }

            var historical = rows.Count;
            var baseline = Math.Max(1, historical - recent.Count);
            var growth = (double)(recent.Count - baseline) / baseline;
            var recentCritical = recent.Count(x => x.Incident.Severity == SeverityLevel.Critical);
            var recentHigh = recent.Count(x => x.Incident.Severity == SeverityLevel.High);
            var people = recent.Sum(x => x.Incident.PeopleAffected ?? 0);
            var score = Math.Min(100, recent.Count * 10)
                * 0.40
                + Math.Min(100, Math.Max(0, growth * 100))
                * 0.25
                + Math.Min(100, recentCritical * 25)
                * 0.20
                + Math.Min(100, recentHigh * 20)
                * 0.10
                + Math.Min(100, people * 5)
                * 0.05;
            var cell = rows[0].Cell;

            results.Add(new HotspotResult
            {
                CellKey = group.Key,
                CenterLatitude = cell.CenterLatitude,
                CenterLongitude = cell.CenterLongitude,
                HotspotScore = Math.Round(score, 2),
                RecentIncidentCount = recent.Count,
                ForecastedIncidentCount = recent.Count,
                CriticalIncidentCount = recentCritical,
                HighIncidentCount = recentHigh,
                GrowthRatio = growth,
                Trend = growth > 0.15 ? "Increasing" :
                    growth < -0.15 ? "Decreasing" : "Stable"
            });
        }

        return results.OrderByDescending(x => x.HotspotScore).ToArray();
    }

    private GeoGridCell GetCell(decimal latitude, decimal longitude)
    {
        var size = _options.CellSizeDegrees;
        var latIndex = Math.Floor((double)latitude / size);
        var lonIndex = Math.Floor((double)longitude / size);
        return new GeoGridCell
        {
            CellKey = $"{latIndex}:{lonIndex}",
            CenterLatitude = (latIndex + 0.5) * size,
            CenterLongitude = (lonIndex + 0.5) * size
        };
    }
}

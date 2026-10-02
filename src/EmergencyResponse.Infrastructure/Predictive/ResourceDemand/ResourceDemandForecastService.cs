using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Application.Predictive.ResourceDemand;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace EmergencyResponse.Infrastructure.Predictive.ResourceDemand;

public sealed class ResourceDemandForecastService : IResourceDemandForecastService
{
    private readonly ApplicationDbContext _db;
    private readonly MLContext _mlContext = new();

    public ResourceDemandForecastService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ResourceDemandForecast> ForecastAsync(
        string resourceType,
        int horizon,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ResourceType>(resourceType, true, out var parsedType))
        {
            return Unavailable(resourceType);
        }

        var end = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour - 1);
        var start = end.AddHours(-335);
        var rows = await _db.Incidents
            .AsNoTracking()
            .Where(x => x.ReportedAtUtc >= start && x.ReportedAtUtc <= end)
            .SelectMany(x => x.IncidentType.ResourceRequirements
                .Where(r => r.ResourceType == parsedType)
                .Select(r => new
                {
                    Bucket = new DateTime(
                        x.ReportedAtUtc.Year,
                        x.ReportedAtUtc.Month,
                        x.ReportedAtUtc.Day,
                        x.ReportedAtUtc.Hour,
                        0,
                        0,
                        DateTimeKind.Utc),
                    Demand = r.PreferredCount
                }))
            .GroupBy(x => x.Bucket)
            .Select(g => new { Bucket = g.Key, Demand = g.Sum(x => x.Demand) })
            .ToDictionaryAsync(x => x.Bucket, x => (float)x.Demand, cancellationToken);

        var observations = TimeSeriesBucketBuilder.FillMissingBuckets(
            rows,
            start,
            end,
            ForecastGranularity.Hour);
        if (observations.Count < 336 || observations.Count(x => x.Value > 0) < 20)
        {
            return Unavailable(resourceType);
        }

        var data = _mlContext.Data.LoadFromEnumerable(
            observations.Select(x => new Input { Value = x.Value }));
        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            nameof(Output.Forecast),
            nameof(Input.Value),
            windowSize: 24,
            seriesLength: 168,
            trainSize: observations.Count,
            horizon: Math.Clamp(horizon, 1, 168),
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: nameof(Output.LowerBound),
            confidenceUpperBoundColumn: nameof(Output.UpperBound));
        var model = pipeline.Fit(data);
        var output = _mlContext.Model
            .CreatePredictionEngine<Input, Output>(model)
            .Predict(new Input());
        var generated = DateTime.UtcNow;
        var points = Enumerable.Range(0, output.Forecast.Length)
            .Select(i => new ForecastPoint
            {
                TimestampUtc = end.AddHours(i + 1),
                Forecast = Math.Max(0, output.Forecast[i]),
                LowerBound = output.LowerBound.ElementAtOrDefault(i),
                UpperBound = output.UpperBound.ElementAtOrDefault(i)
            })
            .ToArray();
        var available = await _db.Resources
            .AsNoTracking()
            .CountAsync(x => x.Type == parsedType && x.Status == ResourceStatus.Available, cancellationToken);
        var peak = (int)Math.Ceiling(points.DefaultIfEmpty().Max(x => x?.Forecast ?? 0));

        return new ResourceDemandForecast
        {
            ResourceType = parsedType.ToString(),
            Points = points,
            CurrentAvailable = available,
            PeakForecastDemand = peak,
            PotentialShortage = peak > available,
            GeneratedAtUtc = generated
        };
    }

    private static ResourceDemandForecast Unavailable(string resourceType)
        => new()
        {
            ResourceType = resourceType,
            GeneratedAtUtc = DateTime.UtcNow
        };

    private sealed class Input
    {
        public float Value { get; set; }
    }

    private sealed class Output
    {
        public float[] Forecast { get; set; } = [];
        public float[] LowerBound { get; set; } = [];
        public float[] UpperBound { get; set; } = [];
    }
}

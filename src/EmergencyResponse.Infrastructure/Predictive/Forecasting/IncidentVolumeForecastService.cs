using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace EmergencyResponse.Infrastructure.Predictive.Forecasting;

public sealed class IncidentVolumeForecastService : IForecastingService
{
    private readonly ApplicationDbContext _db;
    private readonly MLContext _mlContext = new();
    private readonly ForecastTrainingRequirements _requirements = new();

    public IncidentVolumeForecastService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ForecastResult> ForecastAsync(
        ForecastType forecastType,
        string seriesKey,
        ForecastGranularity granularity,
        int horizon,
        CancellationToken cancellationToken = default)
    {
        if (forecastType != ForecastType.IncidentVolume ||
            !string.Equals(seriesKey, "INCIDENT_VOLUME:ALL", StringComparison.OrdinalIgnoreCase))
        {
            return Unavailable(forecastType, seriesKey, granularity, "Unsupported incident-volume series.");
        }

        var end = Floor(DateTime.UtcNow, granularity).AddHours(-1);
        var start = granularity == ForecastGranularity.Day
            ? end.Date.AddDays(-_requirements.MinimumObservations + 1)
            : end.AddHours(-_requirements.MinimumObservations + 1);

        var counts = await _db.Incidents
            .AsNoTracking()
            .Where(x => x.ReportedAtUtc >= start && x.ReportedAtUtc <= end)
            .GroupBy(x => granularity == ForecastGranularity.Day
                ? x.ReportedAtUtc.Date
                : new DateTime(
                    x.ReportedAtUtc.Year,
                    x.ReportedAtUtc.Month,
                    x.ReportedAtUtc.Day,
                    x.ReportedAtUtc.Hour,
                    0,
                    0,
                    DateTimeKind.Utc))
            .Select(g => new { Bucket = g.Key, Count = g.Count() })
            .ToDictionaryAsync(
                x => x.Bucket,
                x => (float)x.Count,
                cancellationToken);

        var observations = TimeSeriesBucketBuilder.FillMissingBuckets(
            counts,
            start,
            end,
            granularity);

        if (observations.Count < _requirements.MinimumObservations ||
            observations.Count(x => x.Value > 0) < _requirements.MinimumNonZeroObservations)
        {
            return Unavailable(
                forecastType,
                seriesKey,
                granularity,
                $"At least {_requirements.MinimumObservations} observations and " +
                $"{_requirements.MinimumNonZeroObservations} non-zero observations are required.");
        }

        var data = _mlContext.Data.LoadFromEnumerable(
            observations.Select(x => new TimeSeriesInput { Value = x.Value }));
        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(TimeSeriesOutput.Forecast),
            inputColumnName: nameof(TimeSeriesInput.Value),
            windowSize: granularity == ForecastGranularity.Hour ? 24 : 7,
            seriesLength: granularity == ForecastGranularity.Hour ? 168 : 28,
            trainSize: observations.Count,
            horizon: Math.Clamp(horizon, 1, 168),
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: nameof(TimeSeriesOutput.LowerBound),
            confidenceUpperBoundColumn: nameof(TimeSeriesOutput.UpperBound));

        var model = pipeline.Fit(data);
        var engine = _mlContext.Model.CreatePredictionEngine<TimeSeriesInput, TimeSeriesOutput>(model);
        var output = engine.Predict(new TimeSeriesInput());
        var generated = DateTime.UtcNow;
        var points = Enumerable.Range(0, output.Forecast.Length)
            .Select(index => new ForecastPoint
            {
                TimestampUtc = end.Add(
                    granularity == ForecastGranularity.Day
                        ? TimeSpan.FromDays(index + 1)
                        : TimeSpan.FromHours(index + 1)),
                Forecast = Math.Max(0, output.Forecast[index]),
                LowerBound = output.LowerBound.ElementAtOrDefault(index),
                UpperBound = output.UpperBound.ElementAtOrDefault(index)
            })
            .ToArray();

        return new ForecastResult
        {
            ForecastType = forecastType,
            Granularity = granularity,
            SeriesKey = seriesKey,
            ModelName = granularity == ForecastGranularity.Hour
                ? "IncidentVolumeHourly"
                : "IncidentVolumeDaily",
            ModelVersion = "v1",
            Points = points,
            GeneratedAtUtc = generated,
            IsAvailable = true
        };
    }

    private static DateTime Floor(DateTime value, ForecastGranularity granularity)
        => granularity == ForecastGranularity.Day
            ? value.Date
            : new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, DateTimeKind.Utc);

    private static ForecastResult Unavailable(
        ForecastType type,
        string seriesKey,
        ForecastGranularity granularity,
        string reason)
        => new()
        {
            ForecastType = type,
            Granularity = granularity,
            SeriesKey = seriesKey,
            GeneratedAtUtc = DateTime.UtcNow,
            IsAvailable = false,
            UnavailableReason = reason
        };

    private sealed class TimeSeriesInput
    {
        public float Value { get; set; }
    }

    private sealed class TimeSeriesOutput
    {
        public float[] Forecast { get; set; } = [];

        public float[] LowerBound { get; set; } = [];

        public float[] UpperBound { get; set; } = [];
    }
}

using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Application.Predictive.Forecasting;

public static class TimeSeriesBucketBuilder
{
    public static IReadOnlyList<TimeSeriesObservation> FillMissingBuckets(
        IReadOnlyDictionary<DateTime, float> values,
        DateTime start,
        DateTime end,
        ForecastGranularity granularity = ForecastGranularity.Hour)
    {
        if (end < start)
        {
            return [];
        }

        var result = new List<TimeSeriesObservation>();
        var increment = granularity == ForecastGranularity.Day
            ? TimeSpan.FromDays(1)
            : TimeSpan.FromHours(1);

        for (var current = start; current <= end; current = current.Add(increment))
        {
            result.Add(new TimeSeriesObservation
            {
                TimestampUtc = current,
                Value = values.TryGetValue(current, out var value) ? value : 0
            });
        }

        return result;
    }
}

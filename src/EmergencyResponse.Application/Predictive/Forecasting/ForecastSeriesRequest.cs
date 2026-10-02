using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Application.Predictive.Forecasting;

public sealed class ForecastSeriesRequest
{
    public ForecastType ForecastType { get; init; }

    public string SeriesKey { get; init; } = string.Empty;

    public ForecastGranularity Granularity { get; init; }

    public int Horizon { get; init; }
}

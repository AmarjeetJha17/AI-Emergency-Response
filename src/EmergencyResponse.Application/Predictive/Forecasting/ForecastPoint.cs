namespace EmergencyResponse.Application.Predictive.Forecasting;

public sealed class ForecastPoint
{
    public DateTime TimestampUtc { get; init; }

    public double Forecast { get; init; }

    public double? LowerBound { get; init; }

    public double? UpperBound { get; init; }
}

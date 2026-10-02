namespace EmergencyResponse.Application.Predictive.Forecasting;

public sealed class TimeSeriesObservation
{
    public DateTime TimestampUtc { get; init; }

    public float Value { get; init; }
}

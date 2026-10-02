namespace EmergencyResponse.Application.Predictive.Forecasting;

public sealed class ForecastTrainingRequirements
{
    public int MinimumObservations { get; init; } = 336;

    public int MinimumNonZeroObservations { get; init; } = 20;
}

using EmergencyResponse.Application.Predictive.Forecasting;

namespace EmergencyResponse.Application.Predictive.ResourceDemand;

public sealed class ResourceDemandForecast
{
    public string ResourceType { get; init; } = string.Empty;

    public IReadOnlyList<ForecastPoint> Points { get; init; } = [];

    public int CurrentAvailable { get; init; }

    public int PeakForecastDemand { get; init; }

    public bool PotentialShortage { get; init; }

    public DateTime GeneratedAtUtc { get; init; }
}

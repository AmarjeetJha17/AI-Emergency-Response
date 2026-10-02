using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Application.Predictive.Forecasting;

public sealed class ForecastResult
{
    public ForecastType ForecastType { get; init; }

    public ForecastGranularity Granularity { get; init; }

    public string SeriesKey { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string ModelVersion { get; init; } = string.Empty;

    public IReadOnlyList<ForecastPoint> Points { get; init; } = [];

    public double? MAE { get; init; }

    public double? RMSE { get; init; }

    public DateTime GeneratedAtUtc { get; init; }

    public bool IsAvailable { get; init; } = true;

    public string? UnavailableReason { get; init; }
}

using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Domain.Entities;

public sealed class OperationalForecast : BaseEntity
{
    public ForecastType ForecastType { get; set; }

    public ForecastGranularity Granularity { get; set; }

    public string SeriesKey { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public string FeatureSchemaVersion { get; set; } = string.Empty;

    public DateTime ForecastGeneratedAtUtc { get; set; }

    public DateTime ForecastStartAtUtc { get; set; }

    public DateTime ForecastEndAtUtc { get; set; }

    public int Horizon { get; set; }

    public string ForecastJson { get; set; } = "[]";

    public string? LowerBoundJson { get; set; }

    public string? UpperBoundJson { get; set; }

    public double? MAE { get; set; }

    public double? RMSE { get; set; }

    public bool HumanReviewed { get; set; }

    public string? HumanDecision { get; set; }
}

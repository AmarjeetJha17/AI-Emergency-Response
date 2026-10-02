using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class ForecastEvaluation : BaseEntity
{
    public Guid OperationalForecastId { get; set; }

    public OperationalForecast OperationalForecast { get; set; } = null!;

    public DateTime EvaluationAtUtc { get; set; }

    public double MAE { get; set; }

    public double RMSE { get; set; }

    public string DetailsJson { get; set; } = "{}";
}

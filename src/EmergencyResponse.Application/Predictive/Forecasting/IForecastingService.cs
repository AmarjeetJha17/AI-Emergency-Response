using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Application.Predictive.Forecasting;

public interface IForecastingService
{
    Task<ForecastResult> ForecastAsync(
        ForecastType forecastType,
        string seriesKey,
        ForecastGranularity granularity,
        int horizon,
        CancellationToken cancellationToken = default);
}

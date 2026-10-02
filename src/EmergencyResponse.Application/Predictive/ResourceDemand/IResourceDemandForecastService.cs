namespace EmergencyResponse.Application.Predictive.ResourceDemand;

public interface IResourceDemandForecastService
{
    Task<ResourceDemandForecast> ForecastAsync(
        string resourceType,
        int horizon,
        CancellationToken cancellationToken = default);
}

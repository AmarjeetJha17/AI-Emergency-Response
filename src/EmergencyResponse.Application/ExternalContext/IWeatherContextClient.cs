namespace EmergencyResponse.Application.ExternalContext;

public interface IWeatherContextClient
{
    Task<WeatherContextData?> GetCurrentAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeatherForecastPoint>> GetHourlyForecastAsync(
        double latitude,
        double longitude,
        int durationHours,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SevereWeatherAlert>> GetSevereAlertsAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}

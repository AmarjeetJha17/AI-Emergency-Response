namespace EmergencyResponse.Application.Predictive.Hotspots;

public interface IHotspotService
{
    Task<IReadOnlyList<HotspotResult>> GetHotspotsAsync(
        int hours,
        CancellationToken cancellationToken = default);
}

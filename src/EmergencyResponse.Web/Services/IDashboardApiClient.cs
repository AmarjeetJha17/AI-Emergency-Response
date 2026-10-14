using EmergencyResponse.Application.Dashboard;
using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Application.Predictive.Hotspots;

namespace EmergencyResponse.Web.Services;

public interface IDashboardApiClient
{
    Task<ForecastResult> GetIncidentVolumeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HotspotResult>> GetHotspotsAsync(CancellationToken cancellationToken = default);

    Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardIncidentItem>> GetIncidentsAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardChartPoint>> GetSeverityAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardTimeSeriesPoint>> GetTrendAsync(
        int days = 7,
        CancellationToken cancellationToken = default);
}

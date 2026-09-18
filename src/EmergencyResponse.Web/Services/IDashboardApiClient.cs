using EmergencyResponse.Application.Dashboard;

namespace EmergencyResponse.Web.Services;

public interface IDashboardApiClient
{
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

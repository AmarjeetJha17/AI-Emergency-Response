namespace EmergencyResponse.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardIncidentItem>> GetRecentIncidentsAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardChartPoint>> GetSeverityDistributionAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardTimeSeriesPoint>> GetIncidentTrendAsync(
        int days = 7,
        CancellationToken cancellationToken = default);
}

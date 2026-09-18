using EmergencyResponse.Application.Dashboard;

namespace EmergencyResponse.Web.Models.Dashboard;

public sealed class DashboardViewModel
{
    public DashboardSummary Summary { get; init; } = new();

    public IReadOnlyList<DashboardIncidentItem> Incidents { get; init; } = [];

    public IReadOnlyList<DashboardChartPoint> SeverityDistribution { get; init; } = [];

    public IReadOnlyList<DashboardTimeSeriesPoint> IncidentTrend { get; init; } = [];
}

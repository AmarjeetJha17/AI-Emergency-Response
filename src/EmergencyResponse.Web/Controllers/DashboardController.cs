using EmergencyResponse.Web.Models.Dashboard;
using EmergencyResponse.Web.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class DashboardController : Controller
{
    private readonly IDashboardApiClient _dashboardApiClient;

    public DashboardController(IDashboardApiClient dashboardApiClient)
    {
        _dashboardApiClient = dashboardApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var summary = await _dashboardApiClient.GetSummaryAsync(
            cancellationToken);

        var incidents = await _dashboardApiClient.GetIncidentsAsync(
            50,
            cancellationToken);

        var severity = await _dashboardApiClient.GetSeverityAsync(
            cancellationToken);

        var trend = await _dashboardApiClient.GetTrendAsync(
            7,
            cancellationToken);

        return View(new DashboardViewModel
        {
            Summary = summary,
            Incidents = incidents,
            SeverityDistribution = severity,
            IncidentTrend = trend
        });
    }
}

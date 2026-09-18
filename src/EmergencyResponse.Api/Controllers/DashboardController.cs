using EmergencyResponse.Application.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _dashboardService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<IReadOnlyList<DashboardIncidentItem>>> GetIncidents(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        return Ok(
            await _dashboardService.GetRecentIncidentsAsync(
                take,
                cancellationToken));
    }

    [HttpGet("severity")]
    public async Task<ActionResult<IReadOnlyList<DashboardChartPoint>>> GetSeverity(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _dashboardService.GetSeverityDistributionAsync(
                cancellationToken));
    }

    [HttpGet("trend")]
    public async Task<ActionResult<IReadOnlyList<DashboardTimeSeriesPoint>>> GetTrend(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        return Ok(
            await _dashboardService.GetIncidentTrendAsync(
                days,
                cancellationToken));
    }
}

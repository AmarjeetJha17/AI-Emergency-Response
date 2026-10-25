using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Api.Common;
using EmergencyResponse.Application.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/copilot")]
[ServiceFilter(typeof(AIEnabledActionFilter))]
[Authorize(Policy = Permissions.CopilotUse)]
public sealed class CopilotController : ControllerBase
{
    private readonly IEmergencyCopilotService _copilot;

    public CopilotController(IEmergencyCopilotService copilot)
    {
        _copilot = copilot;
    }

    [HttpPost("query")]
    [EnableRateLimiting("copilot")]
    public async Task<ActionResult<CopilotResponse>> Query(
        CopilotQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Question))
        {
            return BadRequest("A question is required.");
        }

        return Ok(await _copilot.QueryAsync(query, cancellationToken));
    }
}

using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Copilot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/copilot")]
[Authorize(Roles =
    $"{ApplicationRoles.Responder},{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
public sealed class CopilotController : ControllerBase
{
    private readonly IEmergencyCopilotService _copilot;

    public CopilotController(IEmergencyCopilotService copilot)
    {
        _copilot = copilot;
    }

    [HttpPost("query")]
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

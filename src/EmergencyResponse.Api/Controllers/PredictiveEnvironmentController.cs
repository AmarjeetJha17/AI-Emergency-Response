using EmergencyResponse.Application.ExternalContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/predictive")]
[Authorize(Roles = "Responder,Operator,EmergencyAdmin,SystemAdmin")]
public sealed class PredictiveEnvironmentController : ControllerBase
{
    private readonly IOptions<AzureMapsOptions> _options;

    public PredictiveEnvironmentController(IOptions<AzureMapsOptions> options)
    {
        _options = options;
    }

    [HttpGet("environmental-status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            Provider = "AzureMaps",
            Enabled = _options.Value.Enabled,
            Status = _options.Value.Enabled ? "Configured" : "Disabled",
            Authentication = "Microsoft Entra ID / managed identity",
            CheckedAtUtc = DateTime.UtcNow
        });
    }
}

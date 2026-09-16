using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/ai-test")]
[Authorize(Roles = "SystemAdmin")]
public class AITestController : ControllerBase
{
    private readonly IIncidentAIOrchestrationService _orchestrationService;

    public AITestController(
        IIncidentAIOrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    public sealed record AITestRequest
    {
        public Guid IncidentId { get; init; }

        public IncidentAnalysisInput Input { get; init; } = null!;
    }

    [HttpPost("incident")]
    public async Task<ActionResult<IncidentAnalysisResult>> Analyze(
        AITestRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _orchestrationService
                .AnalyzeAndPersistAsync(
                    request.IncidentId,
                    request.Input,
                    cancellationToken);

        return Ok(result);
    }
}
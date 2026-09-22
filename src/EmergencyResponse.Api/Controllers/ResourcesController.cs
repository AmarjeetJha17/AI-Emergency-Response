using EmergencyResponse.Application.Resources;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/resources")]
[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public ResourcesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResourceDetailsResponse>>> GetResources(
        CancellationToken cancellationToken)
    {
        var resources = await _resourceService.GetResourcesAsync(cancellationToken);
        return Ok(resources);
    }

    [HttpGet("{resourceId:guid}")]
    public async Task<ActionResult<ResourceDetailsResponse>> GetById(
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(resourceId, cancellationToken);
        if (resource is null)
        {
            return NotFound($"Resource with ID {resourceId} was not found.");
        }

        return Ok(resource);
    }

    [HttpPost("{resourceId:guid}/assign")]
    public async Task<ActionResult<ResourceAssignmentResult>> Assign(
        Guid resourceId,
        AssignResourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resourceService.AssignAsync(
                resourceId,
                request.IncidentId,
                request.Notes,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{resourceId:guid}/status")]
    public async Task<ActionResult<ResourceStatusTransitionResult>> ChangeStatus(
        Guid resourceId,
        ChangeResourceStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resourceService.ChangeStatusAsync(
                resourceId,
                request.Status,
                request.IncidentId,
                request.Reason,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{resourceId:guid}/release")]
    public async Task<IActionResult> Release(
        Guid resourceId,
        ReleaseResourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _resourceService.ReleaseAsync(
                resourceId,
                request.Notes,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

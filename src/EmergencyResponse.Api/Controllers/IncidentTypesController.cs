using EmergencyResponse.Application.DTOs.IncidentTypes;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/incident-types")]
[Authorize]
public class IncidentTypesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public IncidentTypesController(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<
            IReadOnlyCollection<IncidentTypeResponse>>>
        GetAll(CancellationToken cancellationToken)
    {
        var result =
            await _dbContext.IncidentTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new IncidentTypeResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code
                })
                .ToListAsync(cancellationToken);

        return Ok(result);
    }
}
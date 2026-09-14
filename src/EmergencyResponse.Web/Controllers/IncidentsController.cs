using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Web.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize]
public class IncidentsController : Controller
{
    private readonly IIncidentApiClient _incidentApiClient;
    private readonly IIncidentTypeApiClient _incidentTypeApiClient;

    public IncidentsController(
        IIncidentApiClient incidentApiClient,
        IIncidentTypeApiClient incidentTypeApiClient)
    {
        _incidentApiClient = incidentApiClient;
        _incidentTypeApiClient = incidentTypeApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        Guid? incidentTypeId = null,
        string? status = null,
        string? severity = null,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _incidentApiClient.GetPagedAsync(
                page,
                pageSize,
                search,
                incidentTypeId,
                status,
                severity,
                cancellationToken);

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var incidentTypes =
            await _incidentTypeApiClient.GetAllAsync(
                cancellationToken);

        ViewBag.IncidentTypes = incidentTypes;

        return View(new CreateIncidentRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.IncidentTypes =
                await _incidentTypeApiClient.GetAllAsync(
                    cancellationToken);

            return View(request);
        }

        try
        {
            var result =
                await _incidentApiClient.CreateAsync(
                    request,
                    cancellationToken);

            if (result is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The incident could not be created.");

                return View(request);
            }

            return RedirectToAction(
                nameof(Details),
                new { id = result.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _incidentApiClient.GetByIdAsync(
                id,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return View(result);
    }
}
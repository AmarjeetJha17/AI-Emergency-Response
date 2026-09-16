using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Web.Models.Incidents;
using EmergencyResponse.Web.Services;

using ApplicationReviewDuplicateIncidentRequest =
    EmergencyResponse.Application.DTOs.Incidents.ReviewDuplicateIncidentRequest;

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

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet]
    public async Task<IActionResult> DuplicateReview(
        Guid id,
        CancellationToken cancellationToken)
    {
        var candidates =
            await _incidentApiClient.GetDuplicatesAsync(
                id,
                cancellationToken);

        var candidate = candidates.FirstOrDefault();

        if (candidate is null || candidate.MatchId == Guid.Empty)
        {
            return NotFound();
        }

        var source =
            await _incidentApiClient.GetByIdAsync(
                id,
                cancellationToken);

        var candidateIncident =
            await _incidentApiClient.GetByIdAsync(
                candidate.IncidentId,
                cancellationToken);

        if (source is null || candidateIncident is null)
        {
            return NotFound();
        }

        var model =
            new DuplicateIncidentReviewViewModel
            {
                MatchId = candidate.MatchId,
                SourceIncidentId = source.Id,
                CandidateIncidentId = candidateIncident.Id,
                SourceIncidentNumber = source.IncidentNumber,
                CandidateIncidentNumber =
                    candidateIncident.IncidentNumber,
                SourceTitle = source.Title,
                CandidateTitle = candidateIncident.Title,
                SourceDescription = source.Description,
                CandidateDescription = candidateIncident.Description,
                SourceLocation = source.Address,
                CandidateLocation = candidateIncident.Address,
                SourceIncidentType = source.IncidentType,
                CandidateIncidentType = candidateIncident.IncidentType,
                SemanticSimilarity = candidate.SemanticSimilarity,
                GeographicSimilarity = candidate.GeographicSimilarity,
                TemporalSimilarity = candidate.TemporalSimilarity,
                IncidentTypeSimilarity =
                    candidate.IncidentTypeSimilarity,
                DuplicateScore = candidate.DuplicateScore,
                DistanceKm = candidate.DistanceKm,
                TimeDifference = candidate.TimeDifference
            };

        return View(model);
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewDuplicate(
        Guid incidentId,
        Guid matchId,
        ApplicationReviewDuplicateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        await _incidentApiClient.ReviewDuplicateAsync(
            incidentId,
            matchId,
            request,
            cancellationToken);

        TempData["SuccessMessage"] =
            "Duplicate review decision saved.";

        return RedirectToAction(
            nameof(Details),
            new { id = incidentId });
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet]
    public async Task<IActionResult> Duplicates(
        Guid id,
        CancellationToken cancellationToken)
    {
        var candidates =
            await _incidentApiClient.GetDuplicatesAsync(
                id,
                cancellationToken);

        return View(candidates);
    }
}
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Web.Models.Incidents;
using EmergencyResponse.Web.Services;

using ApplicationReviewDuplicateIncidentRequest =
    EmergencyResponse.Application.DTOs.Incidents.ReviewDuplicateIncidentRequest;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Web.Controllers;

[Authorize]
public class IncidentsController : Controller
{
    private readonly IIncidentApiClient _incidentApiClient;
    private readonly IIncidentTypeApiClient _incidentTypeApiClient;
    private readonly ISituationApiClient _situationApiClient;

    public IncidentsController(
        IIncidentApiClient incidentApiClient,
        IIncidentTypeApiClient incidentTypeApiClient,
        ISituationApiClient situationApiClient)
    {
        _incidentApiClient = incidentApiClient;
        _incidentTypeApiClient = incidentTypeApiClient;
        _situationApiClient = situationApiClient;
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
                cancellationToken: cancellationToken);

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> MyReports(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _incidentApiClient.GetPagedAsync(
                page,
                pageSize,
                search: null,
                incidentTypeId: null,
                status: null,
                severity: null,
                myReportsOnly: true,
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
        var incident = await _incidentApiClient.GetByIdAsync(
            id,
            cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        var isOperator =
            User.IsInRole("Operator") ||
            User.IsInRole("EmergencyAdmin") ||
            User.IsInRole("SystemAdmin");

        if (isOperator)
        {
            var evidence = incident.VisualEvidence
                .Select(m => new VisualEvidenceViewModel
                {
                    IncidentId = id,
                    MediaId = m.Id,
                    IncidentNumber = incident.IncidentNumber,
                    ContentType = m.ContentType,
                    MediaUrl = Url.Action(
                        "Media",
                        "Incidents",
                        new { incidentId = id, mediaId = m.Id })!,
                    Analysis = m.Analysis
                })
                .ToList();

            ViewBag.VisualEvidence = evidence;

            try
            {
                var allOpenSituations = await _situationApiClient.GetOpenSituationsAsync(cancellationToken);
                ViewBag.OperationalSituations = allOpenSituations
                    .Where(s => s.Incidents.Any(i => i.IncidentId == id))
                    .ToList();
            }
            catch
            {
                ViewBag.OperationalSituations = Array.Empty<OperationalSituationResponse>();
            }
        }

        return View(incident);
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
        try
        {
            await _incidentApiClient.ReviewDuplicateAsync(
                incidentId,
                matchId,
                request,
                cancellationToken);

            TempData["SuccessMessage"] =
                "Duplicate review decision saved.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(
            nameof(Details),
            new { id = incidentId });
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet]
    public async Task<IActionResult> Media(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var result = await _incidentApiClient.GetMediaStreamAsync(
            incidentId,
            mediaId,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return File(
            result.Stream,
            result.ContentType,
            enableRangeProcessing: true);
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet]
    public async Task<IActionResult> VisualReview(
        Guid id,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentApiClient.GetByIdAsync(
            id,
            cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        var analysis = await _incidentApiClient.GetVisualAnalysisAsync(
            id,
            mediaId,
            cancellationToken);

        if (analysis is null)
        {
            return NotFound();
        }

        ViewBag.IncidentId = id;
        ViewBag.MediaId = mediaId;
        ViewBag.IncidentNumber = incident.IncidentNumber;
        ViewBag.MediaUrl = Url.Action(
            "Media",
            "Incidents",
            new { incidentId = id, mediaId });

        return View(analysis);
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VisualReview(
        Guid id,
        Guid mediaId,
        ReviewVisualAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        await _incidentApiClient.ReviewVisualAnalysisAsync(
            id,
            mediaId,
            request,
            cancellationToken);

        TempData["SuccessMessage"] = "Visual analysis review saved.";

        return RedirectToAction(
            nameof(Details),
            new { id });
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

        // Pass the source incident ID so the view can build correct review links.
        ViewBag.SourceIncidentId = id;

        return View(candidates);
    }

    /// <summary>
    /// Proxies a single extracted video frame JPEG from the API.
    /// This action exists so the browser request carries the operator's
    /// bearer token via BearerTokenHandler — frames are protected content.
    /// </summary>
    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet]
    public async Task<IActionResult> Frame(
        Guid incidentId,
        Guid mediaId,
        int frameNumber,
        CancellationToken cancellationToken)
    {
        var result = await _incidentApiClient.GetFrameAsync(
            incidentId,
            mediaId,
            frameNumber,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return File(result.Stream, result.ContentType);
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        string status,
        string? remarks,
        CancellationToken cancellationToken)
    {
        try
        {
            await _incidentApiClient.UpdateStatusAsync(
                id,
                new UpdateIncidentStatusRequest
                {
                    Status = status,
                    Remarks = remarks
                },
                cancellationToken);

            TempData["SuccessMessage"] =
                $"Incident status updated to {status}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
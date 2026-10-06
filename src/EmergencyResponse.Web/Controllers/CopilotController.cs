using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = "Responder,Operator,EmergencyAdmin,SystemAdmin")]
public sealed class CopilotController : Controller
{
    private readonly ICopilotApiClient _client;

    public CopilotController(ICopilotApiClient client)
    {
        _client = client;
    }

    [HttpGet]
    public IActionResult Index(
        Guid? incidentId = null,
        Guid? situationId = null)
    {
        return View(new CopilotPageModel
        {
            IncidentId = incidentId,
            SituationId = situationId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        CopilotPageModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Question))
        {
            ModelState.AddModelError(
                nameof(model.Question),
                "Enter a question.");
            return View(model);
        }

        model.Response = await _client.QueryAsync(
            new CopilotQuery
            {
                Question = model.Question,
                IncidentId = model.IncidentId,
                SituationId = model.SituationId
            },
            cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAction(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.ApproveActionAsync(
                proposalId,
                comment,
                cancellationToken);
            TempData["CopilotActionMessage"] = result.Succeeded
                ? result.Message
                : $"{result.Message} No operational state was reported as changed.";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            TempData["CopilotActionMessage"] =
                "The action could not be approved or executed. Review the current operational state.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectAction(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken)
    {
        try
        {
            await _client.RejectActionAsync(proposalId, comment, cancellationToken);
            TempData["CopilotActionMessage"] = "The Copilot action proposal was rejected.";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            TempData["CopilotActionMessage"] =
                "The action proposal could not be rejected.";
        }

        return RedirectToAction(nameof(Index));
    }
}

public sealed class CopilotPageModel
{
    public string Question { get; set; } = string.Empty;

    public Guid? IncidentId { get; set; }

    public Guid? SituationId { get; set; }

    public CopilotResponse? Response { get; set; }

    public string? ActionMessage { get; set; }
}

using System.Text.Json;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Situations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Situations;

public sealed class SituationAnalysisService : ISituationAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly AIOptions _options;
    private readonly ILogger<SituationAnalysisService> _logger;

    public SituationAnalysisService(
        IChatClient chatClient,
        IOptions<AIOptions> options,
        ILogger<SituationAnalysisService> logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SituationAnalysisResult> AnalyzeSituationAsync(
        SituationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(snapshot);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var response = await _chatClient.GetResponseAsync<SituationAnalysisResult>(
            [
                new ChatMessage(
                    ChatRole.User,
                    prompt)
            ],
            new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<SituationAnalysisResult>()
            },
            cancellationToken: cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Situation AI analysis completed in {ElapsedMs} ms using {Model}.",
            stopwatch.ElapsedMilliseconds,
            _options.Model);

        var result = response.Result;

        if (result is null)
        {
            throw new InvalidOperationException("AI returned no structured situation analysis.");
        }

        ValidateResult(result);

        return result;
    }

    public Task<SituationAnalysisResult> AnalyzeAsync(
        string situationContext,
        CancellationToken cancellationToken = default)
    {
        return AnalyzeSituationAsync(new SituationSnapshot
        {
            SituationNumber = "SIT-PREVIEW",
            IncidentCount = 1,
            Level = Domain.Enums.EscalationLevel.Advisory,
            SituationScore = 10,
            Incidents = []
        }, cancellationToken);
    }

    private static string BuildPrompt(SituationSnapshot snapshot)
    {
        var incidentItems = string.Join("\n\n", snapshot.Incidents.Select((inc, idx) =>
            $"""
            Incident {idx + 1}:
            - Incident Number: {inc.IncidentNumber}
            - Title: {inc.Title}
            - Description: {inc.Description}
            - Incident Type: {inc.IncidentTypeName ?? "Unknown"}
            - Severity: {inc.Severity}
            - Location: ({inc.Latitude:F4}, {inc.Longitude:F4})
            - Reported At UTC: {inc.CreatedAtUtc:u}
            - People Affected: {inc.PeopleAffected?.ToString() ?? "Unknown"}
            - Casualties: {inc.Casualties?.ToString() ?? "Unknown"}
            - Active Escalations: {inc.ActiveEscalationsCount}
            """));

        return $$"""
        You are an operational emergency response awareness assistant.

        Analyze the collection of related active incidents below that have been grouped into an operational situation.

        Your task is to:
        1. Provide a concise, high-level Title describing the overall situation (e.g., "Urban Flash Flooding & Stranded Vehicles - Sector 4").
        2. Provide an operational Summary synthesizing what is occurring across these incidents.
        3. Identify CommonThemes or patterns (e.g., "flash flooding", "gridlock", "structural danger").
        4. Formulate RecommendedFocus areas for incident commanders (advisory only).
        5. State RiskTrajectory: whether the situation appears "Escalating", "Stable", or "De-escalating" based on incident progression and casualty/hazard indicators.
        6. Provide a Confidence score from 0.0 to 1.0.

        Strict Rules:
        - Rely strictly on the supplied incident facts. Do NOT hallucinate extra casualties or unmentioned hazards.
        - Do NOT make automated dispatch orders or public evacuation announcements.
        - All recommendations are strictly advisory for human emergency operators.
        - Return only valid JSON conforming to the requested schema.

        Situation Overview:
        - Situation Number: {{snapshot.SituationNumber}}
        - Total Incidents: {{snapshot.IncidentCount}}
        - Calculated Composite Score: {{snapshot.SituationScore:F2}}
        - Calculated Escalation Level: {{snapshot.Level}}
        - Total People Affected: {{snapshot.PeopleAffected}}
        - Total Casualties: {{snapshot.Casualties}}
        - Resource Shortage Detected: {{snapshot.ResourceShortageDetected}}

        Incidents in Situation:
        {{incidentItems}}
        """;
    }

    private static void ValidateResult(SituationAnalysisResult result)
    {
        if (result.Confidence is < 0 or > 1)
        {
            throw new InvalidOperationException("Confidence must be between 0 and 1.");
        }

        if (string.IsNullOrWhiteSpace(result.SituationTitle) && string.IsNullOrWhiteSpace(result.Title))
        {
            throw new InvalidOperationException("Title must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(result.SituationSummary) && string.IsNullOrWhiteSpace(result.Summary))
        {
            throw new InvalidOperationException("Summary must not be empty.");
        }
    }
}

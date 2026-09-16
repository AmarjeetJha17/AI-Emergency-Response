using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Common.Configuration;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentAnalysisService
    : IIncidentAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly AIOptions _options;
    private readonly ILogger<IncidentAnalysisService> _logger;

    public IncidentAnalysisService(
        IChatClient chatClient,
        IOptions<AIOptions> options,
        ILogger<IncidentAnalysisService> logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IncidentAnalysisResult> AnalyzeAsync(
        IncidentAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(input);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var response =
            await _chatClient.GetResponseAsync<IncidentAnalysisResult>(
                [
                    new ChatMessage(
                        ChatRole.User,
                        prompt)
                ],
                new ChatOptions
                {
                    ResponseFormat =
                        ChatResponseFormat.ForJsonSchema<
                            IncidentAnalysisResult>()
                },
                cancellationToken: cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Incident AI analysis completed in {ElapsedMs} ms using {Model}.",
            stopwatch.ElapsedMilliseconds,
            _options.Model);

        var result = response.Result;

        if (result is null)
        {
            throw new InvalidOperationException(
                "AI returned no structured incident analysis.");
        }

        ValidateResult(result);

        return result;
    }

    private static string BuildPrompt(
        IncidentAnalysisInput input)
    {
        return $$"""
        You are an emergency incident analysis assistant.

        Analyze the incident information below.

        Your task is to:
        - identify the most likely incident type
        - estimate a candidate severity
        - extract people affected
        - extract vehicles affected
        - identify immediate risks
        - identify immediate concerns
        - describe the reported location
        - recommend the most appropriate operational department
        - provide a confidence score from 0 to 1

        Rules:
        1. Use only information present in the supplied incident.
        2. Do not invent facts.
        3. Unknown information should be null or "Unknown" according to the schema.
        4. Do not make final emergency dispatch decisions.
        5. The application's business rules will determine final severity and priority.

        Incident:

        Title:
        {{input.Title}}

        Description:
        {{input.Description}}

        User supplied incident type:
        {{input.IncidentType ?? "Unknown"}}

        Address:
        {{input.Address ?? "Unknown"}}

        City:
        {{input.City ?? "Unknown"}}

        District:
        {{input.District ?? "Unknown"}}

        State:
        {{input.State ?? "Unknown"}}

        Latitude:
        {{input.Latitude}}

        Longitude:
        {{input.Longitude}}

        People affected:
        {{input.PeopleAffected?.ToString() ?? "Unknown"}}

        Vehicles affected:
        {{input.VehiclesAffected?.ToString() ?? "Unknown"}}

        Casualties:
        {{input.Casualties?.ToString() ?? "Unknown"}}
        """;
    }

    private static void ValidateResult(
        IncidentAnalysisResult result)
    {
        if (result.Confidence is < 0 or > 1)
        {
            throw new InvalidOperationException(
                "AI confidence must be between 0 and 1.");
        }

        if (string.IsNullOrWhiteSpace(result.IncidentType))
        {
            throw new InvalidOperationException(
                "AI incident type is empty.");
        }

        if (string.IsNullOrWhiteSpace(
                result.SeverityCandidate))
        {
            throw new InvalidOperationException(
                "AI severity candidate is empty.");
        }
    }
}
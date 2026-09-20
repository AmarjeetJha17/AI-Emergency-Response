using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Azure.Core;
using Azure.Identity;

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Common.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentVisualAnalysisService
    : IIncidentVisualAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly AIOptions _options;
    private readonly ILogger<IncidentVisualAnalysisService> _logger;

    public IncidentVisualAnalysisService(
        HttpClient httpClient,
        IOptions<AIOptions> options,
        ILogger<IncidentVisualAnalysisService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VisualAnalysisResult> AnalyzeImageAsync(
        Stream imageStream,
        string contentType,
        string? incidentContext,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        var base64 = Convert.ToBase64String(memory.ToArray());

        var schemaJson = BuildSchemaJson();
        var prompt = BuildPrompt(incidentContext);

        var requestObj = new
        {
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = $"data:{contentType};base64,{base64}" } }
                    }
                }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "VisualAnalysisResult",
                    strict = true,
                    schema = JsonDocument.Parse(schemaJson).RootElement
                }
            }
        };

        var endpoint = _options.Endpoint.TrimEnd('/');
        var deployment = _options.Deployment;
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-08-01-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        await ApplyAuthenticationAsync(request, cancellationToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestObj),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Azure AI Foundry vision call failed. Status: {StatusCode}, Body: {ResponseBody}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Azure AI Foundry vision call failed with HTTP {response.StatusCode}: {responseBody}");
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Incident visual analysis completed in {ElapsedMs} ms using {Deployment}.",
            stopwatch.ElapsedMilliseconds,
            deployment);

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Vision model returned an empty response.");
        }

        var result = JsonSerializer.Deserialize<VisualAnalysisResult>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result ?? throw new InvalidOperationException("Vision model response could not be parsed.");
    }

    public async Task<VisualAnalysisResult> AnalyzeVideoFramesAsync(
        IReadOnlyList<Application.Media.VideoFrame> frames,
        string? incidentContext,
        CancellationToken cancellationToken = default)
    {
        if (frames.Count == 0)
        {
            throw new InvalidOperationException("No frames were extracted from the video.");
        }

        var schemaJson = BuildVideoSchemaJson();
        var prompt = BuildVideoPrompt(incidentContext, frames);

        var contentParts = new List<object>
        {
            new { type = "text", text = prompt }
        };

        foreach (var frame in frames)
        {
            var base64 = Convert.ToBase64String(frame.ImageBytes);
            contentParts.Add(new
            {
                type = "image_url",
                image_url = new { url = $"data:image/jpeg;base64,{base64}" }
            });
        }

        var requestObj = new
        {
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = contentParts.ToArray()
                }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "VideoAnalysisResult",
                    strict = true,
                    schema = JsonDocument.Parse(schemaJson).RootElement
                }
            }
        };

        var endpoint = _options.Endpoint.TrimEnd('/');
        var deployment = _options.Deployment;
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-08-01-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        await ApplyAuthenticationAsync(request, cancellationToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestObj),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Azure AI Foundry video analysis call failed. Status: {StatusCode}, Body: {ResponseBody}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Azure AI Foundry video analysis call failed with HTTP {response.StatusCode}: {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Vision model returned an empty response.");
        }

        var videoResult = JsonSerializer.Deserialize<VideoAnalysisModelResult>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Vision model response could not be parsed.");

        var frameFindings = videoResult.FrameFindings
            .Select((f, i) => new VisualFrameFinding
            {
                FrameNumber = i,
                TimestampSeconds = frames[i].TimestampSeconds,
                SceneDescription = f.SceneDescription,
                DetectedHazards = f.DetectedHazards,
                EstimatedPeopleAffected = f.EstimatedPeopleAffected,
                EstimatedVehiclesAffected = f.EstimatedVehiclesAffected,
                PeopleAppearTrapped = f.PeopleAppearTrapped,
                RoadAppearsBlocked = f.RoadAppearsBlocked,
                StructuralDamageVisible = f.StructuralDamageVisible,
                FireVisible = f.FireVisible,
                FloodingVisible = f.FloodingVisible,
                SmokeVisible = f.SmokeVisible,
                ImmediateDangerVisible = f.ImmediateDangerVisible,
                Confidence = f.Confidence
            })
            .ToList();

        return VisualAnalysisAggregator.Aggregate(
            frameFindings,
            videoResult.RecommendedResponse,
            videoResult.VisibleEmergencyIndicators);
    }

    private async Task ApplyAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("api-key", _options.ApiKey);
        }
        else
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
                cancellationToken);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }
    }

    private string BuildPrompt(string? incidentContext)
    {
        return $"""
            Analyze this emergency incident image.

            Incident context:
            {incidentContext ?? "No additional incident context provided."}

            Follow the VisualEvidenceAnalysis_v1 policy.
            Report observable evidence only.
            Do not invent identities, locations, casualty counts,
            or facts that cannot reasonably be observed.
            """;
    }

    private static string BuildVideoPrompt(
        string? incidentContext,
        IReadOnlyList<Application.Media.VideoFrame> frames)
    {
        var frameList = string.Join(
            "\n",
            frames.Select(f =>
                $"Frame {f.FrameNumber} timestamp: {f.TimestampSeconds:F1} seconds"));

        return $"""
            Analyze these representative frames from an emergency incident video.

            {frameList}

            Incident context:
            {incidentContext ?? "No additional incident context provided."}

            Analyze each frame independently.

            Then return:
            1. Per-frame observations in frameFindings (one entry per frame, in order)
            2. visibleEmergencyIndicators that appear across the video
            3. recommendedResponse based on the combined evidence

            Do not assume that multiple frames show different people,
            vehicles, or incidents unless the visual evidence supports that.
            Use conservative estimates for counts.
            Report observable evidence only.
            """;
    }

    private static string BuildVideoSchemaJson()
    {
        return """
        {
          "type": "object",
          "properties": {
            "frameFindings": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "sceneDescription": { "type": "string" },
                  "detectedHazards": { "type": "array", "items": { "type": "string" } },
                  "estimatedPeopleAffected": { "type": ["integer", "null"] },
                  "estimatedVehiclesAffected": { "type": ["integer", "null"] },
                  "peopleAppearTrapped": { "type": "boolean" },
                  "roadAppearsBlocked": { "type": "boolean" },
                  "structuralDamageVisible": { "type": "boolean" },
                  "fireVisible": { "type": "boolean" },
                  "floodingVisible": { "type": "boolean" },
                  "smokeVisible": { "type": "boolean" },
                  "immediateDangerVisible": { "type": "boolean" },
                  "confidence": { "type": "number" }
                },
                "required": [
                  "sceneDescription", "detectedHazards",
                  "estimatedPeopleAffected", "estimatedVehiclesAffected",
                  "peopleAppearTrapped", "roadAppearsBlocked",
                  "structuralDamageVisible", "fireVisible",
                  "floodingVisible", "smokeVisible",
                  "immediateDangerVisible", "confidence"
                ],
                "additionalProperties": false
              }
            },
            "visibleEmergencyIndicators": {
              "type": "array",
              "items": { "type": "string" }
            },
            "recommendedResponse": { "type": ["string", "null"] }
          },
          "required": ["frameFindings", "visibleEmergencyIndicators", "recommendedResponse"],
          "additionalProperties": false
        }
        """;
    }

    private sealed class VideoAnalysisModelResult
    {
        public List<FrameFindingModel> FrameFindings { get; init; } = [];
        public List<string> VisibleEmergencyIndicators { get; init; } = [];
        public string? RecommendedResponse { get; init; }
    }

    private sealed class FrameFindingModel
    {
        public string SceneDescription { get; init; } = string.Empty;
        public List<string> DetectedHazards { get; init; } = [];
        public int? EstimatedPeopleAffected { get; init; }
        public int? EstimatedVehiclesAffected { get; init; }
        public bool PeopleAppearTrapped { get; init; }
        public bool RoadAppearsBlocked { get; init; }
        public bool StructuralDamageVisible { get; init; }
        public bool FireVisible { get; init; }
        public bool FloodingVisible { get; init; }
        public bool SmokeVisible { get; init; }
        public bool ImmediateDangerVisible { get; init; }
        public double Confidence { get; init; }
    }

    private static string BuildSchemaJson()
    {
        return """
        {
          "type": "object",
          "properties": {
            "sceneDescription": {
              "type": "string"
            },
            "detectedHazards": {
              "type": "array",
              "items": {
                "type": "string"
              }
            },
            "visibleEmergencyIndicators": {
              "type": "array",
              "items": {
                "type": "string"
              }
            },
            "estimatedPeopleAffected": {
              "type": ["integer", "null"]
            },
            "estimatedVehiclesAffected": {
              "type": ["integer", "null"]
            },
            "peopleAppearTrapped": {
              "type": "boolean"
            },
            "roadAppearsBlocked": {
              "type": "boolean"
            },
            "structuralDamageVisible": {
              "type": "boolean"
            },
            "fireVisible": {
              "type": "boolean"
            },
            "floodingVisible": {
              "type": "boolean"
            },
            "smokeVisible": {
              "type": "boolean"
            },
            "immediateDangerVisible": {
              "type": "boolean"
            },
            "recommendedResponse": {
              "type": ["string", "null"]
            },
            "confidence": {
              "type": "number"
            }
          },
          "required": [
            "sceneDescription",
            "detectedHazards",
            "visibleEmergencyIndicators",
            "estimatedPeopleAffected",
            "estimatedVehiclesAffected",
            "peopleAppearTrapped",
            "roadAppearsBlocked",
            "structuralDamageVisible",
            "fireVisible",
            "floodingVisible",
            "smokeVisible",
            "immediateDangerVisible",
            "recommendedResponse",
            "confidence"
          ],
          "additionalProperties": false
        }
        """;
    }
}
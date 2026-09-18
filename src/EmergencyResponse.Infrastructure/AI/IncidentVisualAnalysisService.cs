#pragma warning disable OPENAI001

using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text.Json;

using Azure.Identity;

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.VisualAnalysis;

using EmergencyResponse.Application.Common.Configuration;

using Microsoft.Extensions.Options;

using OpenAI.Responses;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentVisualAnalysisService
    : IIncidentVisualAnalysisService
{
    private readonly ResponsesClient _client;
    private readonly AIOptions _options;

    public IncidentVisualAnalysisService(
        IOptions<AIOptions> options)
    {
        _options = options.Value;

        var endpoint =
            _options.Endpoint.TrimEnd('/');

        if (!endpoint.EndsWith(
                "/openai/v1",
                StringComparison.OrdinalIgnoreCase))
        {
            endpoint += "/openai/v1";
        }

        var tokenPolicy =
            new BearerTokenPolicy(
                new DefaultAzureCredential(),
                "https://ai.azure.com/.default");

        _client = new ResponsesClient(
            authenticationPolicy: tokenPolicy,
            options: new ResponsesClientOptions
            {
                Endpoint = new Uri(endpoint)
            });
    }

    public async Task<VisualAnalysisResult> AnalyzeImageAsync(
        Stream imageStream,
        string contentType,
        string? incidentContext,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        using var memory = new MemoryStream();

        await imageStream.CopyToAsync(
            memory,
            cancellationToken);

        var imageData =
            BinaryData.FromBytes(
                memory.ToArray());

        var schema =
            BuildSchema();

        var prompt =
            BuildPrompt(incidentContext);

        var options =
            new CreateResponseOptions
            {
                Model = _options.Deployment,

                InputItems =
                {
                    ResponseItem.CreateUserMessageItem(
                    [
                        ResponseContentPart.CreateInputTextPart(
                            prompt),

                        ResponseContentPart.CreateInputImagePart(
                            imageData,
                            contentType)
                    ])
                },

                TextOptions =
                    new ResponseTextOptions
                    {
                        TextFormat =
                            ResponseTextFormat
                                .CreateJsonSchemaFormat(
                                    "VisualAnalysisResult",
                                    schema,
                                    jsonSchemaIsStrict: true)
                    }
            };

        var response =
            await _client.CreateResponseAsync(
                options,
                cancellationToken);

        stopwatch.Stop();

        var responseResult = response.Value;

        var output = string.Concat(
            responseResult.OutputItems
                .OfType<MessageResponseItem>()
                .SelectMany(m => m.Content)
                .OfType<ResponseContentPart>()
                .Where(p => p.Kind == ResponseContentPartKind.OutputText)
                .Select(p => p.Text));

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "Vision model returned an empty response.");
        }

        var result =
            JsonSerializer.Deserialize<VisualAnalysisResult>(
                output,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return result
            ?? throw new InvalidOperationException(
                "Vision model response could not be parsed.");
    }

    public async Task<VisualAnalysisResult> AnalyzeVideoFramesAsync(
        IReadOnlyList<Application.Media.VideoFrame> frames,
        string? incidentContext,
        CancellationToken cancellationToken = default)
    {
        if (frames.Count == 0)
        {
            throw new InvalidOperationException(
                "No frames were extracted from the video.");
        }

        var schema = BuildVideoSchema();
        var prompt = BuildVideoPrompt(incidentContext, frames);

        var contentParts = new List<ResponseContentPart>
        {
            ResponseContentPart.CreateInputTextPart(prompt)
        };

        foreach (var frame in frames)
        {
            contentParts.Add(
                ResponseContentPart.CreateInputImagePart(
                    BinaryData.FromBytes(frame.ImageBytes),
                    "image/jpeg"));
        }

        var options = new CreateResponseOptions
        {
            Model = _options.Deployment,
            InputItems =
            {
                ResponseItem.CreateUserMessageItem(contentParts)
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "VideoAnalysisResult",
                    schema,
                    jsonSchemaIsStrict: true)
            }
        };

        var response = await _client.CreateResponseAsync(
            options,
            cancellationToken);

        var output = string.Concat(
            response.Value.OutputItems
                .OfType<MessageResponseItem>()
                .SelectMany(m => m.Content)
                .OfType<ResponseContentPart>()
                .Where(p => p.Kind == ResponseContentPartKind.OutputText)
                .Select(p => p.Text));

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "Vision model returned an empty response.");
        }

        var videoResult = JsonSerializer.Deserialize<VideoAnalysisModelResult>(
            output,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException(
                "Vision model response could not be parsed.");

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

    private string BuildPrompt(
        string? incidentContext)
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

    private static BinaryData BuildVideoSchema()
    {
        return BinaryData.FromString(
            """
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
            """);
    }

    // Internal model matching the video JSON schema
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

    private static BinaryData BuildSchema()
    {
        return BinaryData.FromString(
            """
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
            """);
    }
}
using System.Diagnostics;
using System.Text.Json;

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.Common;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class VisualAnalysisOrchestrationService
{
    private readonly ApplicationDbContext _db;
    private readonly IIncidentVisualAnalysisService _visualAnalysisService;
    private readonly IVisualReviewPolicy _reviewPolicy;
    private readonly IIncidentMediaStorage _storage;
    private readonly IVideoFrameExtractor _frameExtractor;
    private readonly AIOptions? _aiOptions;

    public VisualAnalysisOrchestrationService(
        ApplicationDbContext db,
        IIncidentVisualAnalysisService visualAnalysisService,
        IVisualReviewPolicy reviewPolicy,
        IIncidentMediaStorage storage,
        IVideoFrameExtractor frameExtractor,
        IOptions<AIOptions>? aiOptions = null)
    {
        _db = db;
        _visualAnalysisService = visualAnalysisService;
        _reviewPolicy = reviewPolicy;
        _storage = storage;
        _frameExtractor = frameExtractor;
        _aiOptions = aiOptions?.Value;
    }

    public async Task<VisualAnalysis> AnalyzeAndPersistAsync(
        Guid incidentMediaId,
        bool isVideo,
        CancellationToken cancellationToken = default)
    {
        var media = await _db.IncidentMedia
            .Include(x => x.Incident)
            .SingleOrDefaultAsync(
                x => x.Id == incidentMediaId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                $"IncidentMedia '{incidentMediaId}' was not found.");

        // Find or create the VisualAnalysis record in Processing state
        var analysis = await _db.VisualAnalyses
            .FirstOrDefaultAsync(
                x => x.IncidentMediaId == incidentMediaId,
                cancellationToken);

        var modelName = _aiOptions?.Model ?? "gpt-5-mini";
        var deployment = _aiOptions?.Deployment ?? "gpt-5-mini";

        if (analysis is null)
        {
            analysis = new VisualAnalysis
            {
                Id = Guid.NewGuid(),
                IncidentMediaId = media.Id,
                IncidentId = media.IncidentId,
                ModelName = modelName,
                ModelVersion = deployment,
                PromptVersion = AIPromptVersions.VisualEvidenceAnalysis,
                Status = VisualAnalysisStatus.Processing,
                HumanReviewed = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.VisualAnalyses.Add(analysis);
        }
        else
        {
            analysis.Status = VisualAnalysisStatus.Processing;
            analysis.FailureReason = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var incidentContext =
            $"{media.Incident.Title}: {media.Incident.Description}";

        var stopwatch = Stopwatch.StartNew();

        Application.AI.VisualAnalysis.VisualAnalysisResult result;
        IReadOnlyList<Application.Media.VideoFrame> extractedFrames =
            Array.Empty<Application.Media.VideoFrame>();
        double? videoDuration = null;

        if (isVideo)
        {
            await using var videoStream = await _storage.OpenReadAsync(
                media.StorageKey,
                cancellationToken);

            extractedFrames = await _frameExtractor
                .ExtractRepresentativeFramesAsync(
                    videoStream,
                    cancellationToken);

            if (extractedFrames.Count > 0)
            {
                videoDuration = extractedFrames[extractedFrames.Count - 1]
                    .TimestampSeconds;
            }

            result = await _visualAnalysisService.AnalyzeVideoFramesAsync(
                extractedFrames,
                incidentContext,
                cancellationToken);
        }
        else
        {
            await using var imageStream = await _storage.OpenReadAsync(
                media.StorageKey,
                cancellationToken);

            result = await _visualAnalysisService.AnalyzeImageAsync(
                imageStream,
                media.ContentType,
                incidentContext,
                cancellationToken);
        }

        stopwatch.Stop();

        // Persist each extracted frame JPEG so operators can view thumbnails.
        var frameStorageKeys = new List<string>(extractedFrames.Count);

        foreach (var frame in extractedFrames)
        {
            var frameKey =
                $"frames/{analysis.Id:N}/{frame.FrameNumber:D4}.jpg";

            using var frameStream =
                new MemoryStream(frame.ImageBytes, writable: false);

            await _storage.SaveWithKeyAsync(
                frameKey,
                frameStream,
                cancellationToken);

            frameStorageKeys.Add(frameKey);
        }

        var rawOutput = JsonSerializer.Serialize(result);

        analysis.Status = _reviewPolicy.RequiresReview(result)
            ? VisualAnalysisStatus.RequiresReview
            : VisualAnalysisStatus.Completed;
        analysis.FindingsJson = JsonSerializer.Serialize(result);
        analysis.RawOutputJson = rawOutput;
        analysis.Confidence = result.Confidence;
        analysis.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        analysis.CompletedAtUtc = DateTime.UtcNow;
        analysis.FramesAnalyzed = extractedFrames.Count > 0
            ? extractedFrames.Count
            : null;
        analysis.VideoDurationSeconds = videoDuration;
        analysis.FrameTimestampsJson = extractedFrames.Count > 0
            ? JsonSerializer.Serialize(
                extractedFrames.Select(f => f.TimestampSeconds).ToList())
            : null;
        analysis.FrameStorageKeysJson = frameStorageKeys.Count > 0
            ? JsonSerializer.Serialize(frameStorageKeys)
            : null;

        media.AIAnalysisCompletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return analysis;
    }
}

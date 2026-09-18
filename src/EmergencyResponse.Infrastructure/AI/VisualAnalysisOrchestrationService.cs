using System.Diagnostics;
using System.Text.Json;

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.Common;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class VisualAnalysisOrchestrationService
{
    private readonly ApplicationDbContext _db;
    private readonly IIncidentVisualAnalysisService _visualAnalysisService;
    private readonly IVisualReviewPolicy _reviewPolicy;
    private readonly IIncidentMediaStorage _storage;
    private readonly IVideoFrameExtractor _frameExtractor;

    public VisualAnalysisOrchestrationService(
        ApplicationDbContext db,
        IIncidentVisualAnalysisService visualAnalysisService,
        IVisualReviewPolicy reviewPolicy,
        IIncidentMediaStorage storage,
        IVideoFrameExtractor frameExtractor)
    {
        _db = db;
        _visualAnalysisService = visualAnalysisService;
        _reviewPolicy = reviewPolicy;
        _storage = storage;
        _frameExtractor = frameExtractor;
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

        // Give the analysis a stable ID before persisting frames so we can
        // build deterministic storage keys based on it.
        var analysisId = Guid.NewGuid();

        // Persist each extracted frame JPEG so operators can view thumbnails.
        var frameStorageKeys = new List<string>(extractedFrames.Count);

        foreach (var frame in extractedFrames)
        {
            var frameKey =
                $"frames/{analysisId:N}/{frame.FrameNumber:D4}.jpg";

            using var frameStream =
                new MemoryStream(frame.ImageBytes, writable: false);

            await _storage.SaveWithKeyAsync(
                frameKey,
                frameStream,
                cancellationToken);

            frameStorageKeys.Add(frameKey);
        }

        var rawOutput = JsonSerializer.Serialize(result);

        var analysis = new VisualAnalysis
        {
            Id = analysisId,
            IncidentMediaId = media.Id,
            IncidentId = media.IncidentId,
            ModelName = "gpt-5-mini",
            ModelVersion = "gpt-5-mini",
            PromptVersion = AIPromptVersions.VisualEvidenceAnalysis,
            Status = _reviewPolicy.RequiresReview(result)
                ? VisualAnalysisStatus.RequiresReview
                : VisualAnalysisStatus.Completed,
            FindingsJson = JsonSerializer.Serialize(result),
            RawOutputJson = rawOutput,
            Confidence = result.Confidence,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            HumanReviewed = false,
            CompletedAtUtc = DateTime.UtcNow,
            FramesAnalyzed = extractedFrames.Count > 0
                ? extractedFrames.Count
                : null,
            VideoDurationSeconds = videoDuration,
            FrameTimestampsJson = extractedFrames.Count > 0
                ? JsonSerializer.Serialize(
                    extractedFrames.Select(f => f.TimestampSeconds).ToList())
                : null,
            FrameStorageKeysJson = frameStorageKeys.Count > 0
                ? JsonSerializer.Serialize(frameStorageKeys)
                : null
        };

        _db.VisualAnalyses.Add(analysis);

        media.AIAnalysisCompletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return analysis;
    }
}

using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Media;

namespace EmergencyResponse.Application.AI;

public interface IIncidentVisualAnalysisService
{
    Task<VisualAnalysisResult> AnalyzeImageAsync(
        Stream imageStream,
        string contentType,
        string? incidentContext,
        CancellationToken cancellationToken = default);

    Task<VisualAnalysisResult> AnalyzeVideoFramesAsync(
        IReadOnlyList<VideoFrame> frames,
        string? incidentContext,
        CancellationToken cancellationToken = default);
}

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Exceptions;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Situations;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class DisabledEmergencyCopilotService : IEmergencyCopilotService
{
    public Task<CopilotResponse> QueryAsync(
        CopilotQuery query,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();
}

public sealed class DisabledIncidentAnalysisService : IIncidentAnalysisService
{
    public Task<IncidentAnalysisResult> AnalyzeAsync(
        IncidentAnalysisInput input,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();
}

public sealed class DisabledIncidentVisualAnalysisService : IIncidentVisualAnalysisService
{
    public Task<VisualAnalysisResult> AnalyzeImageAsync(
        Stream imageStream,
        string contentType,
        string? incidentContext,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();

    public Task<VisualAnalysisResult> AnalyzeVideoFramesAsync(
        IReadOnlyList<VideoFrame> frames,
        string? incidentContext,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();
}

public sealed class DisabledIncidentEmbeddingService : IIncidentEmbeddingService
{
    public Task<ReadOnlyMemory<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();
}

public sealed class DisabledDuplicateIncidentService : IDuplicateIncidentService
{
    public Task GenerateEmbeddingAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();

    public Task<IReadOnlyList<DuplicateIncidentCandidate>> FindCandidatesAsync(
        Guid incidentId,
        int maxCandidates = 20,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();
}

public sealed class DisabledSituationAnalysisService : ISituationAnalysisService
{
    public Task<SituationAnalysisResult> AnalyzeSituationAsync(
        SituationSnapshot snapshot,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();

    public Task<SituationAnalysisResult> AnalyzeAsync(
        string situationContext,
        CancellationToken cancellationToken = default) =>
        throw new AIFeatureDisabledException();
}

using System.Diagnostics;
using System.Text.Json;

using EmergencyResponse.Application.AI.Common;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentAIOrchestrationService
    : IIncidentAIOrchestrationService
{
    private readonly IIncidentAnalysisService _analysisService;
    private readonly IAIAnalysisRepository _repository;

    public IncidentAIOrchestrationService(
        IIncidentAnalysisService analysisService,
        IAIAnalysisRepository repository)
    {
        _analysisService = analysisService;
        _repository = repository;
    }

    public async Task<IncidentAnalysisResult>
        AnalyzeAndPersistAsync(
            Guid incidentId,
            IncidentAnalysisInput input,
            CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var result =
            await _analysisService.AnalyzeAsync(
                input,
                cancellationToken);

        stopwatch.Stop();

        var outputJson =
            JsonSerializer.Serialize(result);

        var analysis = new AIAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,

            ModelName = "gpt-5-mini",

            ModelVersion = "unknown",

            PromptVersion =
                AIPromptVersions.IncidentAnalysis,

            InputReference =
                $"Incident:{incidentId}",

            OutputJson = outputJson,

            Confidence = result.Confidence,

            ExecutionTimeMs =
                stopwatch.ElapsedMilliseconds,

            HumanReviewed = false
        };

        await _repository.AddAsync(
            analysis,
            cancellationToken);

        return result;
    }
}
using EmergencyResponse.Application.AI.DTOs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IIncidentAIOrchestrationService
{
    Task<IncidentAnalysisResult> AnalyzeAndPersistAsync(
        Guid incidentId,
        IncidentAnalysisInput input,
        CancellationToken cancellationToken = default);
}
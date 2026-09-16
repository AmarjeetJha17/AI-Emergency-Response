using EmergencyResponse.Application.AI.DTOs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IIncidentAnalysisService
{
    Task<IncidentAnalysisResult> AnalyzeAsync(
        IncidentAnalysisInput input,
        CancellationToken cancellationToken = default);
}
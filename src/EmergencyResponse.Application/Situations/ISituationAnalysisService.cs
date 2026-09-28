namespace EmergencyResponse.Application.Situations;

public interface ISituationAnalysisService
{
    Task<SituationAnalysisResult> AnalyzeSituationAsync(
        SituationSnapshot snapshot,
        CancellationToken cancellationToken = default);

    Task<SituationAnalysisResult> AnalyzeAsync(
        string situationContext,
        CancellationToken cancellationToken = default);
}

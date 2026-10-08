namespace EmergencyResponse.CopilotEvaluation.Evaluators;

public sealed class CitationEvaluator
{
    public bool IsCorrect(
        string citedSourceId,
        IReadOnlySet<string> retrievedSourceIds,
        IReadOnlySet<string> existingSourceIds,
        bool claimSupportedBySource)
        => !string.IsNullOrWhiteSpace(citedSourceId) &&
           existingSourceIds.Contains(citedSourceId) &&
           retrievedSourceIds.Contains(citedSourceId) &&
           claimSupportedBySource;
}

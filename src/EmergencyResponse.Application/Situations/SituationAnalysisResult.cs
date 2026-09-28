namespace EmergencyResponse.Application.Situations;

public sealed record SituationAnalysisResult
{
    public string SituationTitle { get; init; } = string.Empty;

    public string SituationSummary { get; init; } = string.Empty;

    public List<string> KeyObservations { get; init; } = [];

    public List<string> MainHazards { get; init; } = [];

    public List<string> OperationalConcerns { get; init; } = [];

    public List<string> RecommendedActions { get; init; } = [];

    public string ConfidenceExplanation { get; init; } = string.Empty;

    public double Confidence { get; init; } = 0.85;

    public string Title => !string.IsNullOrWhiteSpace(SituationTitle) ? SituationTitle : "Situation Cluster";

    public string Summary => !string.IsNullOrWhiteSpace(SituationSummary) ? SituationSummary : "Operational summary unavailable.";

    public List<string> CommonThemes => KeyObservations;

    public List<string> RecommendedFocus => RecommendedActions;

    public string RiskTrajectory => !string.IsNullOrWhiteSpace(ConfidenceExplanation) ? ConfidenceExplanation : "Stable";
}

namespace EmergencyResponse.Application.AI.DuplicateDetection;

public static class DuplicateScoreCalculator
{
    public static double Calculate(
        double semanticSimilarity,
        double geographicSimilarity,
        double temporalSimilarity,
        double incidentTypeSimilarity)
    {
        var score =
            DuplicateScoringWeights.Semantic * semanticSimilarity +
            DuplicateScoringWeights.Geographic * geographicSimilarity +
            DuplicateScoringWeights.Temporal * temporalSimilarity +
            DuplicateScoringWeights.IncidentType * incidentTypeSimilarity;

        return Math.Clamp(score, 0.0, 1.0);
    }
}
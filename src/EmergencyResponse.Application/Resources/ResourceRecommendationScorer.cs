namespace EmergencyResponse.Application.Resources;

public static class ResourceRecommendationScorer
{
    public static double Calculate(
        double distanceScore,
        double availabilityScore,
        double departmentScore,
        double suitabilityScore)
    {
        var score =
            (0.50 * distanceScore) +
            (0.20 * availabilityScore) +
            (0.20 * departmentScore) +
            (0.10 * suitabilityScore);

        return Math.Clamp(Math.Round(score * 100.0, 1), 0.0, 100.0);
    }
}

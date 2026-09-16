namespace EmergencyResponse.Application.AI.DuplicateDetection;

public static class GeographicSimilarityCalculator
{
    public static double Calculate(
        double distanceKm,
        double maximumRelevantDistanceKm = 10)
    {
        if (distanceKm <= 0)
        {
            return 1.0;
        }

        if (distanceKm >= maximumRelevantDistanceKm)
        {
            return 0.0;
        }

        return 1.0 -
               (distanceKm / maximumRelevantDistanceKm);
    }
}
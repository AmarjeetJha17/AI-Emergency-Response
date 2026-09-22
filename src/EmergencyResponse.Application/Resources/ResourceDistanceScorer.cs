namespace EmergencyResponse.Application.Resources;

public static class ResourceDistanceScorer
{
    public static double Calculate(double? distanceKm, double maximumDistanceKm = 50.0)
    {
        if (!distanceKm.HasValue)
        {
            return 0.0;
        }

        if (distanceKm <= 0.0)
        {
            return 1.0;
        }

        if (distanceKm >= maximumDistanceKm)
        {
            return 0.0;
        }

        return 1.0 - (distanceKm.Value / maximumDistanceKm);
    }
}

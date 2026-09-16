namespace EmergencyResponse.Application.AI.DuplicateDetection;

public static class TemporalSimilarityCalculator
{
    public static double Calculate(
        TimeSpan difference,
        double maximumRelevantHours = 24)
    {
        var hours = Math.Abs(difference.TotalHours);

        if (hours <= 0)
        {
            return 1.0;
        }

        if (hours >= maximumRelevantHours)
        {
            return 0.0;
        }

        return 1.0 -
               (hours / maximumRelevantHours);
    }
}
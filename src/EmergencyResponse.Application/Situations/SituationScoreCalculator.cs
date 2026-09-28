namespace EmergencyResponse.Application.Situations;

public static class SituationScoreCalculator
{
    public static double Calculate(
        int incidentCount,
        int criticalIncidentCount,
        int highIncidentCount,
        int peopleAffected,
        int casualties)
    {
        var criticalScore = Math.Min(1.0, criticalIncidentCount / 5.0);
        var highScore = Math.Min(1.0, highIncidentCount / 10.0);
        var incidentScore = Math.Min(1.0, incidentCount / 20.0);
        var peopleScore = Math.Min(1.0, peopleAffected / 100.0);
        var casualtyScore = Math.Min(1.0, casualties / 20.0);

        var score =
            (0.40 * criticalScore) +
            (0.25 * highScore) +
            (0.15 * incidentScore) +
            (0.10 * peopleScore) +
            (0.10 * casualtyScore);

        return Math.Clamp(score * 100.0, 0.0, 100.0);
    }
}

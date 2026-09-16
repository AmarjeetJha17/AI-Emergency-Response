namespace EmergencyResponse.Application.AI.DuplicateDetection;

public static class IncidentTypeSimilarityCalculator
{
    public static double Calculate(
        Guid sourceIncidentTypeId,
        Guid candidateIncidentTypeId)
    {
        return sourceIncidentTypeId == candidateIncidentTypeId
            ? 1.0
            : 0.0;
    }
}
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class AIReviewPolicy
    : IAIReviewPolicy
{
    public bool RequiresHumanReview(
        IncidentAnalysisResult analysis)
    {
        if (analysis.Confidence < 0.75)
        {
            return true;
        }

        if (analysis.Facts.ImmediateDanger)
        {
            return true;
        }

        if (analysis.Facts.PeopleTrapped)
        {
            return true;
        }

        if (analysis.Facts.CriticalInfrastructureAffected)
        {
            return true;
        }

        return false;
    }
}
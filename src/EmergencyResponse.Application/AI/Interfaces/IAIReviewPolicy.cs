using EmergencyResponse.Application.AI.DTOs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IAIReviewPolicy
{
    bool RequiresHumanReview(
        IncidentAnalysisResult analysis);
}
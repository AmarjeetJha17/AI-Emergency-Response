using EmergencyResponse.Application.AI.VisualAnalysis;

namespace EmergencyResponse.Application.AI;

public interface IVisualReviewPolicy
{
    bool RequiresReview(VisualAnalysisResult result);
}
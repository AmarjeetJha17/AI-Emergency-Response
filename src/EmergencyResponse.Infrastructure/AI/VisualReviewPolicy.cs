using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.VisualAnalysis;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class VisualReviewPolicy : IVisualReviewPolicy
{
    public bool RequiresReview(VisualAnalysisResult result)
    {
        return result.Confidence < 0.75
               || result.ImmediateDangerVisible
               || result.PeopleAppearTrapped
               || result.StructuralDamageVisible;
    }
}
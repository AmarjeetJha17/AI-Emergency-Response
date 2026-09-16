using EmergencyResponse.Application.Decision.DTOs;

namespace EmergencyResponse.Application.Decision.Interfaces;

public interface IPriorityEngine
{
    PriorityEvaluationResult Evaluate(
        PriorityEvaluationInput input);
}
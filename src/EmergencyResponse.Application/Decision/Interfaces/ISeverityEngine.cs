using EmergencyResponse.Application.Decision.DTOs;

namespace EmergencyResponse.Application.Decision.Interfaces;

public interface ISeverityEngine
{
    SeverityEvaluationResult Evaluate(
        SeverityEvaluationInput input);
}
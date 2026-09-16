using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.Decision.DTOs;

namespace EmergencyResponse.Application.Decision.Interfaces;

public interface IIncidentDecisionService
{
    IncidentDecisionResult Evaluate(
        IncidentAnalysisResult analysis);
}
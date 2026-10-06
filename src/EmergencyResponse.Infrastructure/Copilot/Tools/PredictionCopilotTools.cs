using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Predictive;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class PredictionCopilotTools
{
    private readonly IEscalationRiskPredictionService _service;
    private readonly CopilotToolAuthorization _authorization;

    public PredictionCopilotTools(
        IEscalationRiskPredictionService service,
        CopilotToolAuthorization authorization)
    {
        _service = service;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<CopilotPredictionResult>> GetEscalationRiskAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        CopilotToolValidation.RequiredId(incidentId, nameof(incidentId));
        await _authorization.RequireIncidentAccessAsync(
            incidentId,
            cancellationToken);
        var prediction = await _service.PredictAsync(
            incidentId,
            cancellationToken);
        return CopilotToolResult<CopilotPredictionResult>.Available(
            new CopilotPredictionResult
            {
                IncidentId = prediction.IncidentId,
                PredictedEscalation = prediction.PredictedEscalation,
                Probability = prediction.Probability,
                RiskScore = prediction.RiskScore,
                ModelName = prediction.ModelName,
                ModelVersion = prediction.ModelVersion,
                GeneratedAtUtc = prediction.GeneratedAtUtc,
                ValidUntilUtc = prediction.ValidUntilUtc
            });
    }
}

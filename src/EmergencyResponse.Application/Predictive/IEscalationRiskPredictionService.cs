namespace EmergencyResponse.Application.Predictive;

public interface IEscalationRiskPredictionService
{
    Task<EscalationRiskPrediction> PredictAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EscalationRiskPrediction>> GetHistoryAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}

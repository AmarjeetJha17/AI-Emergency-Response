namespace EmergencyResponse.Application.Predictive.Jobs;

public sealed record PredictiveAnalysisJob(
    Guid IncidentId,
    string TriggerReason = "Default");

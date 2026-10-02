using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Predictive.Training;

public interface IIncidentPredictionSnapshotBuilder
{
    Task<IncidentPredictionSnapshot> BuildAsync(
        Guid incidentId,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken = default);
}

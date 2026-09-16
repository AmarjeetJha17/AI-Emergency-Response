

namespace EmergencyResponse.Application.AI.DuplicateDetection;

public interface IIncidentConsolidationService
{
    Task ConsolidateDuplicateAsync(
        Guid canonicalIncidentId,
        Guid duplicateIncidentId,
        string? notes,
        CancellationToken cancellationToken = default);
}
namespace EmergencyResponse.Application.Incidents;

public interface IIncidentAnnotationService
{
    Task<IncidentAnnotationResult> AddNoteAsync(
        Guid incidentId,
        string content,
        CancellationToken cancellationToken = default);

    Task<IncidentAnnotationResult> AddTagAsync(
        Guid incidentId,
        string tag,
        CancellationToken cancellationToken = default);
}

public sealed record IncidentAnnotationResult(
    Guid IncidentId,
    string Value,
    DateTime CreatedAtUtc);

namespace EmergencyResponse.Application.ExternalContext;

public sealed record ExternalContextJob(
    Guid IncidentId,
    double Latitude,
    double Longitude);

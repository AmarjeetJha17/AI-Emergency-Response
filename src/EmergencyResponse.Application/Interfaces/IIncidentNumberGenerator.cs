namespace EmergencyResponse.Application.Interfaces;

public interface IIncidentNumberGenerator
{
    Task<string> GenerateAsync(
        CancellationToken cancellationToken = default);
}
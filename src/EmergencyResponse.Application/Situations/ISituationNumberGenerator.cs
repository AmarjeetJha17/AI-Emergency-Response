namespace EmergencyResponse.Application.Situations;

public interface ISituationNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}

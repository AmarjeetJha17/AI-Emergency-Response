using EmergencyResponse.Application.Situations;

namespace EmergencyResponse.Infrastructure.Situations;

public sealed class SituationNumberGenerator : ISituationNumberGenerator
{
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var number = $"SIT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(0, 0xFFFFFF):X6}";
        return Task.FromResult(number);
    }
}

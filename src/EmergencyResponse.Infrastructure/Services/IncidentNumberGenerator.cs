using EmergencyResponse.Application.Interfaces;

namespace EmergencyResponse.Infrastructure.Services;

public sealed class IncidentNumberGenerator
    : IIncidentNumberGenerator
{
    public Task<string> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var number =
            $"INC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(0, 0xFFFFFF):X6}";

        return Task.FromResult(number);
    }
}
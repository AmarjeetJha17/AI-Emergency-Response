using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Decision.DTOs;

public sealed record SeverityEvaluationResult
{
    public int Score { get; init; }

    public SeverityLevel Severity { get; init; }

    public IReadOnlyCollection<string> Reasons { get; init; }
        = Array.Empty<string>();
}
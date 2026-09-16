using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Decision.DTOs;

public sealed record PriorityEvaluationResult
{
    public int Score { get; init; }

    public PriorityLevel Priority { get; init; }

    public IReadOnlyCollection<string> Reasons { get; init; }
        = Array.Empty<string>();
}
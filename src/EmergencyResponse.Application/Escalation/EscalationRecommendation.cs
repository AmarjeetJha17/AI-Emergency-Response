using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Escalation;

public sealed record EscalationRecommendation
{
    public Guid IncidentId { get; init; }

    public EscalationType Type { get; init; }

    public EscalationLevel Level { get; init; }

    public string Reason { get; init; } = null!;

    public IReadOnlyList<string> Evidence { get; init; } = [];

    public double Score { get; init; }

    public bool RequiresHumanReview { get; init; } = true;
}

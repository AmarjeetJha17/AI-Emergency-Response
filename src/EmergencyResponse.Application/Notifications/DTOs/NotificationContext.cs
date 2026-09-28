using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Notifications.DTOs;

public sealed record NotificationContext
{
    public Guid? IncidentId { get; init; }

    public string? IncidentNumber { get; init; }

    public Guid? SituationId { get; init; }

    public string? SituationNumber { get; init; }

    public Guid? EscalationId { get; init; }

    public EscalationLevel? SituationLevel { get; init; }

    public EscalationLevel? EscalationLevel { get; init; }

    public EscalationType? EscalationType { get; init; }

    public bool RequiresAIReview { get; init; }

    public bool RequiresDuplicateReview { get; init; }

    public bool RequiresVisualReview { get; init; }

    public bool ResourceShortage { get; init; }

    public bool ImmediateDanger { get; init; }

    public bool PeopleTrapped { get; init; }

    public string? Reason { get; init; }

    public string? Title { get; init; }

    public string? Details { get; init; }
}

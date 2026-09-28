using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;

namespace EmergencyResponse.Application.Notifications;

public sealed class NotificationRuleEngine : INotificationRuleEngine
{
    public IReadOnlyList<NotificationRequest> Evaluate(NotificationContext context)
    {
        var requests = new List<NotificationRequest>();

        // 1. Escalation Alert Opened
        if (context.EscalationId.HasValue && context.EscalationLevel.HasValue)
        {
            var priority = context.EscalationLevel switch
            {
                EscalationLevel.Critical => NotificationPriority.Critical,
                EscalationLevel.High => NotificationPriority.Urgent,
                EscalationLevel.Elevated => NotificationPriority.Important,
                _ => NotificationPriority.Normal
            };

            requests.Add(new NotificationRequest
            {
                Type = NotificationType.EscalationOpened,
                Priority = priority,
                Title = $"Escalation Alert: {context.EscalationType?.ToString() ?? "Incident"} ({context.EscalationLevel})",
                Message = context.Reason ?? $"Escalation alert opened for incident {context.IncidentNumber ?? string.Empty}.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                IncidentEscalationId = context.EscalationId,
                DeduplicationKey = $"EscalationOpened:{context.EscalationId}"
            });
        }

        // 2. People Trapped
        if (context.PeopleTrapped && context.IncidentId.HasValue)
        {
            requests.Add(new NotificationRequest
            {
                Type = NotificationType.IncidentSeverityChanged,
                Priority = NotificationPriority.Critical,
                Title = $"CRITICAL: People Trapped Reported ({context.IncidentNumber ?? "Incident"})",
                Message = context.Details ?? "Immediate rescue intervention required: occupants reported trapped.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                DeduplicationKey = $"PeopleTrapped:{context.IncidentId}"
            });
        }

        // 3. Immediate Danger
        if (context.ImmediateDanger && context.IncidentId.HasValue)
        {
            requests.Add(new NotificationRequest
            {
                Type = NotificationType.IncidentSeverityChanged,
                Priority = NotificationPriority.Urgent,
                Title = $"URGENT: Immediate Danger Detected ({context.IncidentNumber ?? "Incident"})",
                Message = context.Details ?? "Active immediate threat to life or critical structures detected.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                DeduplicationKey = $"ImmediateDanger:{context.IncidentId}"
            });
        }

        // 4. Operational Situation Level
        if (context.SituationId.HasValue && context.SituationLevel.HasValue)
        {
            if (context.SituationLevel == EscalationLevel.Critical)
            {
                requests.Add(new NotificationRequest
                {
                    Type = NotificationType.SituationLevelChanged,
                    Priority = NotificationPriority.Critical,
                    Title = $"CRITICAL SITUATION: {context.SituationNumber ?? "Situation"}",
                    Message = context.Reason ?? "Operational situation escalated to Critical level across multiple incident fronts.",
                    TargetRole = ApplicationRoles.Operator,
                    OperationalSituationId = context.SituationId,
                    DeduplicationKey = $"CriticalSituation:{context.SituationId}"
                });
            }
            else
            {
                requests.Add(new NotificationRequest
                {
                    Type = NotificationType.SituationCreated,
                    Priority = NotificationPriority.Important,
                    Title = $"Operational Situation: {context.SituationNumber ?? "Situation"}",
                    Message = context.Reason ?? $"New operational cluster identified with level {context.SituationLevel}.",
                    TargetRole = ApplicationRoles.Operator,
                    OperationalSituationId = context.SituationId,
                    DeduplicationKey = $"SituationCreated:{context.SituationId}"
                });
            }
        }

        // 5. Resource Shortage
        if (context.ResourceShortage)
        {
            var keyId = context.SituationId ?? context.IncidentId ?? Guid.NewGuid();
            requests.Add(new NotificationRequest
            {
                Type = NotificationType.ResourceShortage,
                Priority = NotificationPriority.Urgent,
                Title = "URGENT: Resource Shortage Detected",
                Message = context.Details ?? "Required response units exceed currently available field resources.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                OperationalSituationId = context.SituationId,
                DeduplicationKey = $"ResourceShortage:{keyId}"
            });
        }

        // 6. AI Review Required
        if (context.RequiresAIReview && context.IncidentId.HasValue)
        {
            requests.Add(new NotificationRequest
            {
                Type = NotificationType.AIReviewRequired,
                Priority = NotificationPriority.Important,
                Title = $"AI Review Required: {context.IncidentNumber ?? "Incident"}",
                Message = context.Details ?? "Automated assessment flagged low confidence or safety concerns requiring operator validation.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                DeduplicationKey = $"AIReview:{context.IncidentId}"
            });
        }

        // 7. Duplicate Review Required
        if (context.RequiresDuplicateReview && context.IncidentId.HasValue)
        {
            requests.Add(new NotificationRequest
            {
                Type = NotificationType.DuplicateReviewRequired,
                Priority = NotificationPriority.Important,
                Title = $"Duplicate Review Required: {context.IncidentNumber ?? "Incident"}",
                Message = context.Details ?? "High-confidence duplicate candidate identified requiring operator consolidation.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                DeduplicationKey = $"DuplicateReview:{context.IncidentId}"
            });
        }

        // 8. Visual Review Required
        if (context.RequiresVisualReview && context.IncidentId.HasValue)
        {
            requests.Add(new NotificationRequest
            {
                Type = NotificationType.VisualReviewRequired,
                Priority = NotificationPriority.Important,
                Title = $"Visual Review Required: {context.IncidentNumber ?? "Incident"}",
                Message = context.Details ?? "New visual evidence analysis completed and requires human inspection.",
                TargetRole = ApplicationRoles.Operator,
                IncidentId = context.IncidentId,
                DeduplicationKey = $"VisualReview:{context.IncidentId}"
            });
        }

        return requests;
    }
}

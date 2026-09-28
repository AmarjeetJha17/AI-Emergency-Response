using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Escalation;

public static class EscalationStatusRules
{
    public static bool IsAllowed(
        EscalationStatus current,
        EscalationStatus next)
    {
        return current switch
        {
            EscalationStatus.Open =>
                next is
                    EscalationStatus.Acknowledged or
                    EscalationStatus.Resolved or
                    EscalationStatus.Dismissed,

            EscalationStatus.Acknowledged =>
                next is
                    EscalationStatus.Resolved or
                    EscalationStatus.Dismissed,

            EscalationStatus.Resolved =>
                false,

            EscalationStatus.Dismissed =>
                false,

            _ => false
        };
    }
}

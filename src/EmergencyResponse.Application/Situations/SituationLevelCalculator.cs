using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Situations;

public static class SituationLevelCalculator
{
    public static EscalationLevel Calculate(double score)
    {
        return score switch
        {
            >= 75.0 => EscalationLevel.Critical,
            >= 50.0 => EscalationLevel.High,
            >= 25.0 => EscalationLevel.Elevated,
            _ => EscalationLevel.Advisory
        };
    }
}

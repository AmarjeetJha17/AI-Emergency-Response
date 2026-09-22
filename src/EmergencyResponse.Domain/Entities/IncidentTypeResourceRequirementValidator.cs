namespace EmergencyResponse.Domain.Entities;

public static class IncidentTypeResourceRequirementValidator
{
    public static void Validate(IncidentTypeResourceRequirement requirement)
    {
        if (requirement.MinimumCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requirement.MinimumCount), "MinimumCount cannot be negative.");
        }

        if (requirement.PreferredCount < requirement.MinimumCount)
        {
            throw new ArgumentException("PreferredCount cannot be less than MinimumCount.");
        }

        if (requirement.PriorityOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requirement.PriorityOrder), "PriorityOrder must be at least 1.");
        }
    }
}

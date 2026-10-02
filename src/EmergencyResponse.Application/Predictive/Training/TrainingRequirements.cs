namespace EmergencyResponse.Application.Predictive.Training;

public sealed class TrainingRequirements
{
    public int MinimumRows { get; init; } = 500;

    public int MinimumPositiveRows { get; init; } = 50;

    public int MinimumNegativeRows { get; init; } = 50;
}

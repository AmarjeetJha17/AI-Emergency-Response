namespace EmergencyResponse.Application.Predictive.Training;

public sealed class ModelAcceptancePolicy
{
    public double MinimumRecall { get; init; } = 0.70;

    public double MinimumPrecision { get; init; } = 0.20;

    public double MinimumAuc { get; init; } = 0.65;
}

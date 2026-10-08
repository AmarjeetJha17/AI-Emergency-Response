using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Training;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class TrainingDatasetValidatorTests
{
    [Fact]
    public void Validator_RejectsIncompleteWindowsAndInsufficientClasses()
    {
        var cutoff = DateTime.UtcNow;
        var rows = new[]
        {
            new TrainingDatasetRow
            {
                IncidentId = Guid.NewGuid(),
                SnapshotAtUtc = cutoff.AddHours(-1),
                FeatureHash = "hash",
                Features = new EscalationPredictionFeatures(),
                Label = false
            }
        };

        var result = new TrainingDatasetValidator().Validate(
            rows,
            cutoff,
            6,
            new TrainingRequirements
            {
                MinimumRows = 2,
                MinimumPositiveRows = 1,
                MinimumNegativeRows = 1
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("positive rows"));
        Assert.Contains(result.Errors, x => x.Contains("future label window"));
    }
}

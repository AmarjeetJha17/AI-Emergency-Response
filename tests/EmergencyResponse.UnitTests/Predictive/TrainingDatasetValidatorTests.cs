using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Training;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class TrainingDatasetValidatorTests
{
    private static readonly DateTime Cutoff = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

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

    [Fact]
    public void Validator_accepts_complete_balanced_chronologically_sorted_dataset()
    {
        var rows = new[]
        {
            CreateRow(Cutoff.AddDays(-6), label: true),
            CreateRow(Cutoff.AddDays(-5), label: false)
        };

        var result = new TrainingDatasetValidator().Validate(
            rows,
            Cutoff,
            6,
            new TrainingRequirements
            {
                MinimumRows = 2,
                MinimumPositiveRows = 1,
                MinimumNegativeRows = 1
            });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(0, result.DuplicateRows);
        Assert.Equal(0, result.MissingFeatureRows);
        Assert.Equal(0, result.IncompleteWindowRows);
    }

    [Fact]
    public void Validator_reports_insufficient_negative_rows()
    {
        var rows = new[]
        {
            CreateRow(Cutoff.AddDays(-7), label: true),
            CreateRow(Cutoff.AddDays(-6), label: true)
        };

        var result = new TrainingDatasetValidator().Validate(
            rows,
            Cutoff,
            6,
            new TrainingRequirements
            {
                MinimumRows = 2,
                MinimumPositiveRows = 1,
                MinimumNegativeRows = 1
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("0 negative rows; minimum is 1"));
    }

    [Fact]
    public void Validator_counts_duplicate_rows_and_reports_them()
    {
        var first = CreateRow(Cutoff.AddDays(-7), label: true, incidentId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var duplicate = new TrainingDatasetRow
        {
            IncidentId = first.IncidentId,
            SnapshotAtUtc = first.SnapshotAtUtc,
            FeatureHash = first.FeatureHash,
            Features = new EscalationPredictionFeatures(),
            Label = false
        };

        var result = new TrainingDatasetValidator().Validate(
            [first, duplicate],
            Cutoff,
            6,
            new TrainingRequirements { MinimumRows = 1, MinimumPositiveRows = 0, MinimumNegativeRows = 0 });

        Assert.Equal(1, result.DuplicateRows);
        Assert.Contains(result.Errors, x => x.Contains("1 duplicate rows"));
    }

    [Fact]
    public void Validator_counts_missing_feature_hash_and_features()
    {
        var rows = new[]
        {
            new TrainingDatasetRow
            {
                IncidentId = Guid.NewGuid(),
                SnapshotAtUtc = Cutoff.AddDays(-7),
                FeatureHash = " ",
                Features = new EscalationPredictionFeatures()
            },
            new TrainingDatasetRow
            {
                IncidentId = Guid.NewGuid(),
                SnapshotAtUtc = Cutoff.AddDays(-6),
                FeatureHash = "present",
                Features = null!
            }
        };

        var result = new TrainingDatasetValidator().Validate(
            rows,
            Cutoff,
            6,
            new TrainingRequirements { MinimumRows = 0, MinimumPositiveRows = 0, MinimumNegativeRows = 0 });

        Assert.Equal(2, result.MissingFeatureRows);
        Assert.Contains(result.Errors, x => x.Contains("2 rows with missing feature provenance"));
    }

    [Fact]
    public void Validator_reports_rows_out_of_chronological_order()
    {
        var rows = new[]
        {
            CreateRow(Cutoff.AddDays(-6), label: false),
            CreateRow(Cutoff.AddDays(-7), label: true)
        };

        var result = new TrainingDatasetValidator().Validate(
            rows,
            Cutoff,
            6,
            new TrainingRequirements { MinimumRows = 0, MinimumPositiveRows = 0, MinimumNegativeRows = 0 });

        Assert.Contains(result.Errors, x => x.Contains("not sorted chronologically"));
    }

    private static TrainingDatasetRow CreateRow(
        DateTime snapshotAtUtc,
        bool label,
        Guid? incidentId = null) => new()
    {
        IncidentId = incidentId ?? Guid.NewGuid(),
        SnapshotAtUtc = snapshotAtUtc,
        FeatureHash = Guid.NewGuid().ToString("N"),
        Features = new EscalationPredictionFeatures(),
        Label = label
    };
}

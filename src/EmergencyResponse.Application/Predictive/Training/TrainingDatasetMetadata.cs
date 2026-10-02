namespace EmergencyResponse.Application.Predictive.Training;

public sealed class TrainingDatasetMetadata
{
    public string DatasetVersion { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public DateTime MinSnapshotAtUtc { get; init; }

    public DateTime MaxSnapshotAtUtc { get; init; }

    public int TotalRows { get; init; }

    public int PositiveRows { get; init; }

    public int NegativeRows { get; init; }

    public string FeatureSchemaVersion { get; init; } = string.Empty;

    public int PredictionWindowHours { get; init; }
}

namespace EmergencyResponse.Application.Predictive.Training;

public sealed class ModelManifest
{
    public string ModelName { get; init; } = string.Empty;

    public string ModelVersion { get; init; } = string.Empty;

    public string DatasetVersion { get; init; } = string.Empty;

    public string FeatureSchemaVersion { get; init; } = string.Empty;

    public string PredictionTarget { get; init; } = string.Empty;

    public int PredictionWindowHours { get; init; }

    public DateTime TrainedAtUtc { get; init; }

    public DateTime TrainingDataStartUtc { get; init; }

    public DateTime TrainingDataEndUtc { get; init; }

    public int TrainingRows { get; init; }

    public int ValidationRows { get; init; }

    public int TestRows { get; init; }

    public double Accuracy { get; init; }

    public double F1 { get; init; }

    public double Precision { get; init; }

    public double Recall { get; init; }

    public double Auc { get; init; }

    public string Algorithm { get; init; } = string.Empty;
}

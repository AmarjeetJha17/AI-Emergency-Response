namespace EmergencyResponse.Application.Predictive.Training;

public sealed class TrainingDatasetValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = [];

    public int DuplicateRows { get; init; }

    public int MissingFeatureRows { get; init; }

    public int IncompleteWindowRows { get; init; }
}

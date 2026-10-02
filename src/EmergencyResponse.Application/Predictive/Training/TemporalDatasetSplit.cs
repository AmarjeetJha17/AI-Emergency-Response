namespace EmergencyResponse.Application.Predictive.Training;

public sealed class TemporalDatasetSplit<T>
{
    public IReadOnlyList<T> Train { get; init; } = [];

    public IReadOnlyList<T> Validation { get; init; } = [];

    public IReadOnlyList<T> Test { get; init; } = [];
}

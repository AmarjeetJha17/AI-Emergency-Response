using EmergencyResponse.PredictiveTraining.Training;

namespace EmergencyResponse.PredictiveTraining.Data;

public sealed class HistoricalDatasetExportResult
{
    public IReadOnlyList<EscalationModelInput> Rows { get; init; } = [];

    public DateTime DataCutoffUtc { get; init; }
}

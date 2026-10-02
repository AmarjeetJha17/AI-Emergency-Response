using EmergencyResponse.Application.Predictive.Escalation;

namespace EmergencyResponse.Application.Predictive.Training;

public sealed class TrainingDatasetValidator
{
    public TrainingDatasetValidationResult Validate(
        IReadOnlyList<TrainingDatasetRow> rows,
        DateTime dataCutoffUtc,
        int predictionWindowHours,
        TrainingRequirements requirements)
    {
        var errors = new List<string>();
        var duplicateRows = rows
            .GroupBy(x => new { x.IncidentId, x.SnapshotAtUtc, x.FeatureHash })
            .Sum(group => Math.Max(0, group.Count() - 1));
        var missingFeatureRows = rows.Count(x =>
            string.IsNullOrWhiteSpace(x.FeatureHash) ||
            x.Features is null);
        var incompleteWindowRows = rows.Count(x =>
            x.SnapshotAtUtc > dataCutoffUtc.AddHours(-predictionWindowHours));

        if (rows.Count < requirements.MinimumRows)
        {
            errors.Add($"Dataset contains {rows.Count} rows; minimum is {requirements.MinimumRows}.");
        }

        var positiveRows = rows.Count(x => x.Label);
        var negativeRows = rows.Count - positiveRows;
        if (positiveRows < requirements.MinimumPositiveRows)
        {
            errors.Add($"Dataset contains {positiveRows} positive rows; minimum is {requirements.MinimumPositiveRows}.");
        }

        if (negativeRows < requirements.MinimumNegativeRows)
        {
            errors.Add($"Dataset contains {negativeRows} negative rows; minimum is {requirements.MinimumNegativeRows}.");
        }

        if (duplicateRows > 0)
        {
            errors.Add($"Dataset contains {duplicateRows} duplicate rows.");
        }

        if (missingFeatureRows > 0)
        {
            errors.Add($"Dataset contains {missingFeatureRows} rows with missing feature provenance.");
        }

        if (incompleteWindowRows > 0)
        {
            errors.Add($"Dataset contains {incompleteWindowRows} rows without a complete future label window.");
        }

        if (rows.OrderBy(x => x.SnapshotAtUtc)
            .Select(x => x.SnapshotAtUtc)
            .SequenceEqual(rows.Select(x => x.SnapshotAtUtc)) is false)
        {
            errors.Add("Dataset rows are not sorted chronologically.");
        }

        return new TrainingDatasetValidationResult
        {
            Errors = errors,
            DuplicateRows = duplicateRows,
            MissingFeatureRows = missingFeatureRows,
            IncompleteWindowRows = incompleteWindowRows
        };
    }
}

namespace EmergencyResponse.Application.Predictive.Training;

public static class TemporalDatasetSplitter
{
    public static TemporalDatasetSplit<T> Split<T>(
        IReadOnlyList<T> rows,
        Func<T, DateTime> timestampSelector)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(timestampSelector);

        var ordered = rows
            .OrderBy(timestampSelector)
            .ToList();

        var trainEnd = (int)(ordered.Count * 0.70);
        var validationEnd = (int)(ordered.Count * 0.85);

        return new TemporalDatasetSplit<T>
        {
            Train = ordered.Take(trainEnd).ToList(),
            Validation = ordered
                .Skip(trainEnd)
                .Take(validationEnd - trainEnd)
                .ToList(),
            Test = ordered.Skip(validationEnd).ToList()
        };
    }
}

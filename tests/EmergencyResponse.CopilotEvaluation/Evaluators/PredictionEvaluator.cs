namespace EmergencyResponse.CopilotEvaluation.Evaluators;

using EmergencyResponse.CopilotEvaluation.Infrastructure;

public sealed class PredictionEvaluator
{
    public ForecastScore EvaluateForecast(
        IReadOnlyList<double> actual,
        IReadOnlyList<double> predicted)
    {
        if (actual.Count == 0 || actual.Count != predicted.Count)
        {
            throw new ArgumentException("Actual and predicted series must have the same non-zero length.");
        }

        var errors = actual.Zip(predicted, (a, p) => a - p).ToArray();
        return new ForecastScore
        {
            MeanAbsoluteError = errors.Average(Math.Abs),
            RootMeanSquaredError = Math.Sqrt(errors.Average(error => error * error))
        };
    }

    public ClassificationScore EvaluateClassification(
        IReadOnlyList<bool> expected,
        IReadOnlyList<bool> predicted)
    {
        if (expected.Count == 0 || expected.Count != predicted.Count)
        {
            throw new ArgumentException("Expected and predicted labels must have the same non-zero length.");
        }

        var binary = BinaryMetric.From(expected.Zip(predicted, (e, p) => (e, p)).ToArray());
        return new ClassificationScore
        {
            Precision = binary.Precision,
            Recall = binary.Recall,
            F1 = binary.F1,
            Accuracy = (double)(binary.TruePositive + binary.TrueNegative) / binary.Total
        };
    }

    public ClassificationScore EvaluateClassification(
        IReadOnlyList<bool> expected,
        IReadOnlyList<double> predictedProbability,
        double decisionThreshold = 0.5)
    {
        if (expected.Count == 0 || expected.Count != predictedProbability.Count ||
            predictedProbability.Any(score => !double.IsFinite(score) || score is < 0 or > 1) ||
            !double.IsFinite(decisionThreshold) || decisionThreshold is < 0 or > 1)
        {
            throw new ArgumentException("Expected labels and probabilities must have the same non-zero length, with probabilities in [0,1].");
        }

        var thresholded = predictedProbability.Select(score => score >= decisionThreshold).ToArray();
        var classification = EvaluateClassification(expected, thresholded);
        var positives = expected.Count(label => label);
        var negatives = expected.Count - positives;
        if (positives == 0 || negatives == 0)
        {
            return classification with
            {
                RocAuc = null,
                PrAuc = positives == 0 ? 0 : 1,
                BrierScore = expected.Zip(predictedProbability, (actual, score) => Math.Pow((actual ? 1d : 0d) - score, 2)).Average()
            };
        }

        var positiveNegativePairs = (
            from positive in Enumerable.Range(0, expected.Count).Where(index => expected[index])
            from negative in Enumerable.Range(0, expected.Count).Where(index => !expected[index])
            select predictedProbability[positive].CompareTo(predictedProbability[negative]) switch
            {
                > 0 => 1d,
                0 => 0.5d,
                _ => 0d
            }).ToArray();
        var ordered = Enumerable.Range(0, expected.Count)
            .OrderByDescending(index => predictedProbability[index])
            .ThenBy(index => index)
            .ToArray();
        var truePositives = 0;
        var precisionAtPositive = new List<double>();
        for (var rank = 0; rank < ordered.Length; rank++)
        {
            if (!expected[ordered[rank]]) continue;
            truePositives++;
            precisionAtPositive.Add((double)truePositives / (rank + 1));
        }

        return classification with
        {
            RocAuc = positiveNegativePairs.Average(),
            PrAuc = precisionAtPositive.Average(),
            BrierScore = expected.Zip(predictedProbability, (actual, score) => Math.Pow((actual ? 1d : 0d) - score, 2)).Average()
        };
    }
}

public sealed class ForecastScore
{
    public double MeanAbsoluteError { get; init; }
    public double RootMeanSquaredError { get; init; }
}

public sealed record ClassificationScore
{
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1 { get; init; }
    public double Accuracy { get; init; }
    public double? RocAuc { get; init; }
    public double? PrAuc { get; init; }
    public double? BrierScore { get; init; }
}

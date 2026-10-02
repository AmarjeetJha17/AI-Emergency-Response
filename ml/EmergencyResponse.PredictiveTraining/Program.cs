using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.PredictiveTraining.Data;
using EmergencyResponse.PredictiveTraining.Training;
using Microsoft.Data.SqlClient;
using Microsoft.ML;

namespace EmergencyResponse.PredictiveTraining;

public static class Program
{
    private const int PredictionWindowHours = 6;

    public static async Task<int> Main(string[] args)
    {
        var options = ParseArguments(args);
        var datasetVersion = GetOption(options, "dataset-version", "escalation-risk-dataset-v1")!;
        var modelVersion = GetOption(options, "model-version", "v1")!;
        var connectionString = GetOption(
            options,
            "connection-string",
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine(
                "A SQL Server connection string is required via --connection-string " +
                "or ConnectionStrings__DefaultConnection.");
            return 2;
        }

        var repositoryRoot = FindRepositoryRoot();
        if (repositoryRoot is null)
        {
            Console.Error.WriteLine("Could not locate the repository root.");
            return 2;
        }

        var root = repositoryRoot;
        var generatedDataDirectory = Path.Combine(root, "ml", "data", "generated");
        var reportsDirectory = Path.Combine(root, "ml", "data", "reports");
        var modelDirectory = Path.Combine(
            root,
            "ml",
            "Models",
            "escalation-risk",
            modelVersion);
        Directory.CreateDirectory(generatedDataDirectory);
        Directory.CreateDirectory(reportsDirectory);
        Directory.CreateDirectory(modelDirectory);

        var stopwatch = Stopwatch.StartNew();
        var exporter = new HistoricalDatasetExporter();
        HistoricalDatasetExportResult export;
        try
        {
            export = await exporter.ExportAsync(
                connectionString,
                datasetVersion,
                PredictionWindowHours);
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            Console.Error.WriteLine(
                "Training aborted: the historical snapshot table is missing. " +
                "Apply the AddPredictionTrainingSnapshots migration first.");
            Console.Error.WriteLine(ex.Message);
            return 6;
        }
        var rows = export.Rows;

        if (rows.Count == 0)
        {
            Console.Error.WriteLine(
                "Training aborted: SQL Server contains no complete historical prediction snapshots.");
            return 3;
        }

        var validator = new TrainingDatasetValidator();
        var validation = validator.Validate(
            rows.Select(ToTrainingRow).ToList(),
            export.DataCutoffUtc,
            PredictionWindowHours,
            new TrainingRequirements());

        var metadata = HistoricalDatasetExporter.CreateMetadata(
            rows,
            datasetVersion,
            PredictionWindowHours);
        var datasetPath = Path.Combine(
            generatedDataDirectory,
            $"{datasetVersion}.csv");
        WriteDatasetCsv(rows, datasetPath);

        if (!validation.IsValid)
        {
            var invalidReport = new
            {
                Dataset = metadata,
                DataQuality = validation,
                Errors = validation.Errors
            };
            await WriteJsonAsync(
                Path.Combine(reportsDirectory, $"{datasetVersion}-rejected.json"),
                invalidReport);
            Console.Error.WriteLine("Training aborted because the dataset failed validation:");
            foreach (var error in validation.Errors)
            {
                Console.Error.WriteLine($"- {error}");
            }

            return 4;
        }

        var split = TemporalDatasetSplitter.Split(
            rows,
            x => x.SnapshotAtUtc);
        var mlContext = new MLContext(seed: 42);
        var candidateResults = new[]
        {
            TrainCandidate(mlContext, "SdcaLogisticRegression", split.Train, split.Validation),
            TrainCandidate(mlContext, "FastTree", split.Train, split.Validation)
        };

        var acceptance = new ModelAcceptancePolicy();
        var selected = candidateResults
            .Where(x =>
                x.Metrics.PositiveRecall >= acceptance.MinimumRecall &&
                x.Metrics.PositivePrecision >= acceptance.MinimumPrecision &&
                x.Metrics.AreaUnderRocCurve >= acceptance.MinimumAuc)
            .OrderByDescending(x => x.Metrics.F1Score)
            .ThenByDescending(x => x.Metrics.AreaUnderRocCurve)
            .FirstOrDefault();

        if (selected is null)
        {
            Console.Error.WriteLine(
                "Training aborted: no candidate satisfied the model acceptance policy.");
            return 5;
        }

        var finalPredictions = selected.Model.Transform(
            mlContext.Data.LoadFromEnumerable(split.Test));
        var finalMetrics = mlContext.BinaryClassification.Evaluate(
            finalPredictions,
            labelColumnName: nameof(EscalationModelInput.Label));
        var baseline = CalculateBaseline(split.Test);

        var modelPath = Path.Combine(modelDirectory, "model.zip");
        mlContext.Model.Save(
            selected.Model,
            mlContext.Data.LoadFromEnumerable(split.Train).Schema,
            modelPath);

        var manifest = new ModelManifest
        {
            ModelName = "EscalationRisk",
            ModelVersion = modelVersion,
            DatasetVersion = datasetVersion,
            FeatureSchemaVersion = FeatureSchemaVersions.EscalationRiskV1,
            PredictionTarget = "EscalationWithinSixHours",
            PredictionWindowHours = PredictionWindowHours,
            TrainedAtUtc = DateTime.UtcNow,
            TrainingDataStartUtc = metadata.MinSnapshotAtUtc,
            TrainingDataEndUtc = metadata.MaxSnapshotAtUtc,
            TrainingRows = split.Train.Count,
            ValidationRows = split.Validation.Count,
            TestRows = split.Test.Count,
            Accuracy = finalMetrics.Accuracy,
            F1 = finalMetrics.F1Score,
            Precision = finalMetrics.PositivePrecision,
            Recall = finalMetrics.PositiveRecall,
            Auc = finalMetrics.AreaUnderRocCurve,
            Algorithm = selected.Algorithm
        };

        await WriteJsonAsync(
            Path.Combine(modelDirectory, "manifest.json"),
            manifest);
        await WriteJsonAsync(
            Path.Combine(root, "ml", "Models", "escalation-risk", "current.json"),
            new { modelName = "EscalationRisk", activeVersion = modelVersion });
        var apiModelDirectory = Path.Combine(
            root,
            "src",
            "EmergencyResponse.Api",
            "models",
            "escalation-risk",
            modelVersion);
        Directory.CreateDirectory(apiModelDirectory);
        File.Copy(
            modelPath,
            Path.Combine(apiModelDirectory, "model.zip"),
            overwrite: true);
        File.Copy(
            Path.Combine(modelDirectory, "manifest.json"),
            Path.Combine(apiModelDirectory, "manifest.json"),
            overwrite: true);
        await WriteJsonAsync(
            Path.Combine(
                root,
                "src",
                "EmergencyResponse.Api",
                "models",
                "escalation-risk",
                "current.json"),
            new { modelName = "EscalationRisk", activeVersion = modelVersion });

        var report = new
        {
            Dataset = metadata,
            DataQuality = validation,
            Training = new
            {
                Algorithm = selected.Algorithm,
                DurationMilliseconds = stopwatch.ElapsedMilliseconds,
                Candidates = candidateResults.Select(x => new
                {
                    x.Algorithm,
                    Validation = ToMetricReport(x.Metrics),
                    x.Confusion
                })
            },
            Validation = ToMetricReport(selected.Metrics),
            Test = ToMetricReport(finalMetrics),
            Baseline = baseline,
            Acceptance = acceptance,
            Manifest = manifest
        };
        await WriteJsonAsync(
            Path.Combine(reportsDirectory, $"{modelVersion}-report.json"),
            report);

        Console.WriteLine(
            $"Published {manifest.ModelName} {manifest.ModelVersion} using {manifest.Algorithm}.");
        Console.WriteLine(
            $"Test AUC={manifest.Auc:F4}, Recall={manifest.Recall:F4}, F1={manifest.F1:F4}.");
        return 0;
    }

    private static CandidateResult TrainCandidate(
        MLContext mlContext,
        string algorithm,
        IReadOnlyList<EscalationModelInput> train,
        IReadOnlyList<EscalationModelInput> validation)
    {
        var featureColumns = new[]
        {
            nameof(EscalationModelInput.SeverityScore),
            nameof(EscalationModelInput.PriorityScore),
            nameof(EscalationModelInput.PeopleAffected),
            nameof(EscalationModelInput.VehiclesAffected),
            nameof(EscalationModelInput.Casualties),
            nameof(EscalationModelInput.ImmediateDanger),
            nameof(EscalationModelInput.PeopleTrapped),
            nameof(EscalationModelInput.CriticalInfrastructureAffected),
            nameof(EscalationModelInput.InfrastructureDamage),
            nameof(EscalationModelInput.RoadBlocked),
            nameof(EscalationModelInput.EscalationCount),
            nameof(EscalationModelInput.DuplicateCount),
            nameof(EscalationModelInput.RelatedIncidentCount),
            nameof(EscalationModelInput.ResourceShortage),
            nameof(EscalationModelInput.IncidentAgeMinutes)
        };
        var featurePipeline = mlContext.Transforms
            .Concatenate("Features", featureColumns)
            .Append(mlContext.Transforms.NormalizeMinMax("Features"));
        IEstimator<ITransformer> pipeline = algorithm switch
        {
            "FastTree" => featurePipeline.Append(
                mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: nameof(EscalationModelInput.Label),
                    featureColumnName: "Features")),
            _ => featurePipeline.Append(
                mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: nameof(EscalationModelInput.Label),
                    featureColumnName: "Features"))
        };

        var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(train));
        var predictions = model.Transform(mlContext.Data.LoadFromEnumerable(validation));
        var metrics = mlContext.BinaryClassification.Evaluate(
            predictions,
            labelColumnName: nameof(EscalationModelInput.Label));
        var confusion = CalculateConfusion(mlContext, predictions);
        return new CandidateResult(algorithm, model, metrics, confusion);
    }

    private static object ToMetricReport(
        Microsoft.ML.Data.BinaryClassificationMetrics metrics)
    {
        return JsonSerializer.Serialize(new
        {
            metrics.Accuracy,
            Precision = metrics.PositivePrecision,
            Recall = metrics.PositiveRecall,
            F1 = metrics.F1Score,
            Auc = metrics.AreaUnderRocCurve,
        });
    }

    private static object CalculateConfusion(
        MLContext mlContext,
        IDataView predictions)
    {
        var values = mlContext.Data
            .CreateEnumerable<PredictionRow>(predictions, reuseRowObject: false)
            .ToList();
        return new
        {
            TrueNegative = values.Count(x => !x.Label && !x.PredictedLabel),
            FalseNegative = values.Count(x => x.Label && !x.PredictedLabel),
            FalsePositive = values.Count(x => !x.Label && x.PredictedLabel),
            TruePositive = values.Count(x => x.Label && x.PredictedLabel)
        };
    }

    private static object CalculateBaseline(
        IReadOnlyList<EscalationModelInput> rows)
    {
        var positives = rows.Count(x => x.Label);
        return new
        {
            Accuracy = rows.Count == 0 ? 0 : (double)(rows.Count - positives) / rows.Count,
            Precision = 0.0,
            Recall = 0.0,
            F1 = 0.0,
            Auc = 0.5
        };
    }

    private static TrainingDatasetRow ToTrainingRow(EscalationModelInput row)
    {
        return new TrainingDatasetRow
        {
            IncidentId = row.IncidentId,
            SnapshotAtUtc = row.SnapshotAtUtc,
            OutcomeAtUtc = row.OutcomeAtUtc,
            FeatureHash = row.FeatureHash,
            Label = row.Label,
            Features = new EmergencyResponse.Application.Predictive.Escalation.EscalationPredictionFeatures
            {
                SeverityScore = row.SeverityScore,
                PriorityScore = row.PriorityScore,
                PeopleAffected = row.PeopleAffected,
                VehiclesAffected = row.VehiclesAffected,
                Casualties = row.Casualties,
                ImmediateDanger = row.ImmediateDanger,
                PeopleTrapped = row.PeopleTrapped,
                CriticalInfrastructureAffected = row.CriticalInfrastructureAffected,
                InfrastructureDamage = row.InfrastructureDamage,
                RoadBlocked = row.RoadBlocked,
                EscalationCount = row.EscalationCount,
                DuplicateCount = row.DuplicateCount,
                RelatedIncidentCount = row.RelatedIncidentCount,
                ResourceShortage = row.ResourceShortage,
                IncidentAgeMinutes = row.IncidentAgeMinutes
            }
        };
    }

    private static void WriteDatasetCsv(
        IEnumerable<EscalationModelInput> rows,
        string path)
    {
        using var writer = new StreamWriter(path, false);
        writer.WriteLine(
            "SnapshotAtUtc,IncidentId,OutcomeAtUtc,FeatureHash,SeverityScore," +
            "PriorityScore,PeopleAffected,VehiclesAffected,Casualties,ImmediateDanger," +
            "PeopleTrapped,CriticalInfrastructureAffected,InfrastructureDamage,RoadBlocked," +
            "EscalationCount,DuplicateCount,RelatedIncidentCount,ResourceShortage," +
            "IncidentAgeMinutes,Label");
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
                row.SnapshotAtUtc.ToString("O", CultureInfo.InvariantCulture),
                row.IncidentId,
                row.OutcomeAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                row.FeatureHash,
                row.SeverityScore.ToString(CultureInfo.InvariantCulture),
                row.PriorityScore.ToString(CultureInfo.InvariantCulture),
                row.PeopleAffected.ToString(CultureInfo.InvariantCulture),
                row.VehiclesAffected.ToString(CultureInfo.InvariantCulture),
                row.Casualties.ToString(CultureInfo.InvariantCulture),
                row.ImmediateDanger.ToString(CultureInfo.InvariantCulture),
                row.PeopleTrapped.ToString(CultureInfo.InvariantCulture),
                row.CriticalInfrastructureAffected.ToString(CultureInfo.InvariantCulture),
                row.InfrastructureDamage.ToString(CultureInfo.InvariantCulture),
                row.RoadBlocked.ToString(CultureInfo.InvariantCulture),
                row.EscalationCount.ToString(CultureInfo.InvariantCulture),
                row.DuplicateCount.ToString(CultureInfo.InvariantCulture),
                row.RelatedIncidentCount.ToString(CultureInfo.InvariantCulture),
                row.ResourceShortage.ToString(CultureInfo.InvariantCulture),
                row.IncidentAgeMinutes.ToString(CultureInfo.InvariantCulture),
                row.Label));
        }
    }

    private static async Task WriteJsonAsync(string path, object value)
    {
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(
                value,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal) &&
                !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                result[args[i][2..]] = args[i + 1];
                i++;
            }
        }

        return result;
    }

    private static string? GetOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        string? fallback)
    {
        return options.TryGetValue(name, out var value) ? value : fallback;
    }

    private static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AI-Emergency-Response.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private sealed record CandidateResult(
        string Algorithm,
        ITransformer Model,
        Microsoft.ML.Data.BinaryClassificationMetrics Metrics,
        object Confusion);

    private sealed class PredictionRow
    {
        public bool Label { get; set; }

        public bool PredictedLabel { get; set; }
    }
}

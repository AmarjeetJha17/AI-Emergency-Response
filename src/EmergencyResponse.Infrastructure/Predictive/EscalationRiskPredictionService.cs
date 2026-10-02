using System.Diagnostics;
using System.Text.Json;
using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Predictive;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace EmergencyResponse.Infrastructure.Predictive;

public sealed class EscalationRiskPredictionService : IEscalationRiskPredictionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEscalationPredictionFeatureBuilder _featureBuilder;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EscalationRiskPredictionService> _logger;

    private static readonly object _modelLock = new();
    private static ITransformer? _cachedModel;
    private static MLContext? _mlContext;
    private static string? _loadedModelPath;

    public EscalationRiskPredictionService(
        ApplicationDbContext dbContext,
        IEscalationPredictionFeatureBuilder featureBuilder,
        IConfiguration configuration,
        ILogger<EscalationRiskPredictionService> logger)
    {
        _dbContext = dbContext;
        _featureBuilder = featureBuilder;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EscalationRiskPrediction> PredictAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        using var activity = EmergencyResponseTelemetry.ActivitySource.StartActivity("AI.PredictiveAnalysis");
        activity?.SetTag("emergency.incident.id", incidentId.ToString());

        var stopwatch = Stopwatch.StartNew();
        var snapshotTime = DateTime.UtcNow;

        var snapshot = await _featureBuilder.BuildSnapshotAsync(
            incidentId,
            snapshotTime,
            cancellationToken);
        var historicalSnapshot = new IncidentPredictionSnapshot
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            PredictionType = PredictionType.EscalationRisk,
            SnapshotAtUtc = snapshot.SnapshotAtUtc,
            FeatureSnapshotJson = JsonSerializer.Serialize(snapshot.Features),
            LabelEvaluationAtUtc = snapshotTime,
            FeatureHash = snapshot.FeatureHash
        };
        _dbContext.IncidentPredictionSnapshots.Add(historicalSnapshot);

        var modelName = _configuration["Predictive:ModelName"] ?? "escalation-risk-v1";
        var modelVersion = _configuration["Predictive:ModelVersion"] ?? "1.0.0";
        var datasetVersion = _configuration["Predictive:DatasetVersion"]
            ?? "escalation-risk-dataset-v1";
        var featureSchemaVersion = _configuration["Predictive:FeatureSchemaVersion"]
            ?? "incident-escalation-features-v1";

        var (model, error) = EnsureModelLoaded();
        ApplyLoadedManifest(ref modelName, ref modelVersion, ref datasetVersion, ref featureSchemaVersion);

        if (model is null)
        {
            _logger.LogWarning("Predictive model could not be loaded: {Error}. Recording fallback prediction.", error);
            EmergencyResponseTelemetry.PredictionsFailed.Add(1);

            var fallbackEntity = new PredictiveAnalysis
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                PredictionType = PredictionType.EscalationRisk,
                Status = PredictionStatus.Failed,
                ModelName = modelName,
                ModelVersion = modelVersion,
                DatasetVersion = datasetVersion,
                FeatureSchemaVersion = featureSchemaVersion,
                FeatureSnapshotJson = JsonSerializer.Serialize(snapshot),
                FeatureProvenanceJson = BuildFeatureProvenance(snapshot),
                Probability = null,
                RiskScore = null,
                PredictedEscalation = null,
                FailureReason = error ?? "Model artifact not available",
                GeneratedAtUtc = snapshotTime,
                ValidUntilUtc = null,
                HumanReviewed = false
            };

            _dbContext.PredictiveAnalyses.Add(fallbackEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new EscalationRiskPrediction
            {
                IncidentId = incidentId,
                PredictedEscalation = false,
                Probability = 0.0,
                RiskScore = 0.0,
                ModelName = modelName,
                ModelVersion = modelVersion,
                DatasetVersion = datasetVersion,
                FeatureSchemaVersion = featureSchemaVersion,
                GeneratedAtUtc = snapshotTime,
                ValidUntilUtc = null,
                Status = "Failed",
                FailureReason = error,
                FeatureSnapshotJson = fallbackEntity.FeatureSnapshotJson,
                SignalsConsidered = BuildSignalsDictionary(snapshot.Features)
            };
        }

        // Run inference
        float probability;
        bool predictedLabel;
        float rawScore;

        try
        {
            var input = MapToModelInput(snapshot.Features);
            var output = RunInference(model, input);

            predictedLabel = output.PredictedLabel;
            probability = output.Probability;
            rawScore = output.Score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inference failed for incident {IncidentId}", incidentId);
            EmergencyResponseTelemetry.PredictionsFailed.Add(1);

            var failedEntity = new PredictiveAnalysis
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                PredictionType = PredictionType.EscalationRisk,
                Status = PredictionStatus.Failed,
                ModelName = modelName,
                ModelVersion = modelVersion,
                DatasetVersion = datasetVersion,
                FeatureSchemaVersion = featureSchemaVersion,
                FeatureSnapshotJson = JsonSerializer.Serialize(snapshot),
                FeatureProvenanceJson = BuildFeatureProvenance(snapshot),
                FailureReason = ex.Message,
                GeneratedAtUtc = snapshotTime,
                ValidUntilUtc = null,
                HumanReviewed = false
            };

            _dbContext.PredictiveAnalyses.Add(failedEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new EscalationRiskPrediction
            {
                IncidentId = incidentId,
                PredictedEscalation = false,
                Probability = 0.0,
                RiskScore = 0.0,
                ModelName = modelName,
                ModelVersion = modelVersion,
                DatasetVersion = datasetVersion,
                FeatureSchemaVersion = featureSchemaVersion,
                GeneratedAtUtc = snapshotTime,
                Status = "Failed",
                FailureReason = ex.Message,
                FeatureSnapshotJson = failedEntity.FeatureSnapshotJson,
                SignalsConsidered = BuildSignalsDictionary(snapshot.Features)
            };
        }

        stopwatch.Stop();
        EmergencyResponseTelemetry.PredictionDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        EmergencyResponseTelemetry.PredictionsCompleted.Add(1);

        var probDouble = Math.Clamp((double)probability, 0.0, 1.0);
        var riskScore = Math.Round(probDouble * 100.0, 2);
        var validUntil = snapshotTime.AddHours(6);

        var analysisEntity = new PredictiveAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            PredictionType = PredictionType.EscalationRisk,
            Status = probDouble >= 0.70 ? PredictionStatus.RequiresReview : PredictionStatus.Completed,
            ModelName = modelName,
            ModelVersion = modelVersion,
            DatasetVersion = datasetVersion,
            FeatureSchemaVersion = featureSchemaVersion,
            FeatureSnapshotJson = JsonSerializer.Serialize(snapshot),
            FeatureProvenanceJson = BuildFeatureProvenance(snapshot),
            PredictionJson = JsonSerializer.Serialize(new { PredictedLabel = predictedLabel, Probability = probDouble, Score = rawScore }),
            Probability = probDouble,
            RiskScore = riskScore,
            PredictedEscalation = predictedLabel,
            GeneratedAtUtc = snapshotTime,
            ValidUntilUtc = validUntil,
            HumanReviewed = false
        };

        _dbContext.PredictiveAnalyses.Add(analysisEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        activity?.SetTag("emergency.prediction.probability", probDouble.ToString("F4"));
        activity?.SetTag("emergency.prediction.risk_score", riskScore.ToString("F2"));

        return new EscalationRiskPrediction
        {
            IncidentId = incidentId,
            PredictedEscalation = predictedLabel,
            Probability = probDouble,
            RiskScore = riskScore,
            ModelName = modelName,
            ModelVersion = modelVersion,
            DatasetVersion = datasetVersion,
            FeatureSchemaVersion = featureSchemaVersion,
            GeneratedAtUtc = snapshotTime,
            ValidUntilUtc = validUntil,
            Status = analysisEntity.Status.ToString(),
            FeatureSnapshotJson = analysisEntity.FeatureSnapshotJson,
            SignalsConsidered = BuildSignalsDictionary(snapshot.Features)
        };
    }

    public async Task<IReadOnlyList<EscalationRiskPrediction>> GetHistoryAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.PredictiveAnalyses
            .AsNoTracking()
            .Where(p => p.IncidentId == incidentId && p.PredictionType == PredictionType.EscalationRisk)
            .OrderByDescending(p => p.GeneratedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return records.Select(p =>
        {
            IReadOnlyDictionary<string, double> signals = new Dictionary<string, double>();
            if (!string.IsNullOrWhiteSpace(p.FeatureSnapshotJson))
            {
                try
                {
                    var snapshot = JsonSerializer.Deserialize<PredictionFeatureSnapshot>(p.FeatureSnapshotJson);
                    if (snapshot != null)
                    {
                        signals = BuildSignalsDictionary(snapshot.Features);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to parse feature snapshot for prediction {PredictionId}",
                        p.Id);
                }
            }

            return new EscalationRiskPrediction
            {
                IncidentId = p.IncidentId,
                PredictedEscalation = p.PredictedEscalation ?? false,
                Probability = p.Probability ?? 0.0,
                RiskScore = p.RiskScore ?? 0.0,
                ModelName = p.ModelName,
                ModelVersion = p.ModelVersion,
                DatasetVersion = p.DatasetVersion,
                FeatureSchemaVersion = p.FeatureSchemaVersion,
                GeneratedAtUtc = p.GeneratedAtUtc,
                ValidUntilUtc = p.ValidUntilUtc,
                Status = p.Status.ToString(),
                FailureReason = p.FailureReason,
                FeatureSnapshotJson = p.FeatureSnapshotJson,
                SignalsConsidered = signals
            };
        }).ToList();
    }

    private (ITransformer? Model, string? Error) EnsureModelLoaded()
    {
        lock (_modelLock)
        {
            var modelPathConfig = _configuration["Predictive:ModelPath"]
                ?? "models/escalation-risk/current.json";
            modelPathConfig = ResolveActiveModelPath(modelPathConfig);
            var resolvedPath = ResolveModelPath(modelPathConfig);

            if (_cachedModel != null &&
                string.Equals(_loadedModelPath, resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                return (_cachedModel, null);
            }

            if (!File.Exists(resolvedPath))
            {
                return (null, $"Model artifact not found at '{resolvedPath}'. Run PredictiveTraining project first.");
            }

            try
            {
                _mlContext = new MLContext(seed: 42);
                _cachedModel = _mlContext.Model.Load(resolvedPath, out _);
                _loadedModelPath = resolvedPath;
                _logger.LogInformation("Successfully loaded predictive model from {Path}", resolvedPath);
                return (_cachedModel, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load predictive model from {Path}", resolvedPath);
                return (null, ex.Message);
            }
        }
    }

    private static string ResolveModelPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, configuredPath),
            Path.Combine(AppContext.BaseDirectory, "models", Path.GetFileName(configuredPath)),
            Path.Combine(Directory.GetCurrentDirectory(), configuredPath),
            Path.Combine(Directory.GetCurrentDirectory(), "models", Path.GetFileName(configuredPath)),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "EmergencyResponse.Api", "models", Path.GetFileName(configuredPath)),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "EmergencyResponse.Api", "models", Path.GetFileName(configuredPath)),
            Path.Combine(Directory.GetCurrentDirectory(), "ml", "EmergencyResponse.PredictiveTraining", "bin", "Debug", "net10.0", "Models", Path.GetFileName(configuredPath))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static string ResolveActiveModelPath(string configuredPath)
    {
        if (!configuredPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return configuredPath;
        }

        var pointerCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, configuredPath),
            Path.Combine(Directory.GetCurrentDirectory(), configuredPath),
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "src",
                "EmergencyResponse.Api",
                "models",
                "escalation-risk",
                "current.json")
        };

        foreach (var pointerPath in pointerCandidates)
        {
            if (!File.Exists(pointerPath))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(pointerPath));
            var activeVersion = document.RootElement
                .GetProperty("activeVersion")
                .GetString();
            if (!string.IsNullOrWhiteSpace(activeVersion))
            {
                return Path.Combine(
                    Path.GetDirectoryName(pointerPath)!,
                    activeVersion,
                    "model.zip");
            }
        }

        return configuredPath;
    }

    private static void ApplyLoadedManifest(
        ref string modelName,
        ref string modelVersion,
        ref string datasetVersion,
        ref string featureSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(_loadedModelPath))
        {
            return;
        }

        var manifestPath = Path.Combine(
            Path.GetDirectoryName(_loadedModelPath)!,
            "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<ModelManifest>(
                File.ReadAllText(manifestPath));
            if (manifest is null)
            {
                return;
            }

            modelName = manifest.ModelName;
            modelVersion = manifest.ModelVersion;
            datasetVersion = manifest.DatasetVersion;
            featureSchemaVersion = manifest.FeatureSchemaVersion;
        }
        catch (JsonException)
        {
            // The model remains usable when optional manifest metadata is unavailable.
        }
        catch (IOException)
        {
            // The model remains usable when optional manifest metadata is unavailable.
        }
    }

    private static InternalModelOutput RunInference(ITransformer model, InternalModelInput input)
    {
        lock (_modelLock)
        {
            _mlContext ??= new MLContext(seed: 42);
            var engine = _mlContext.Model.CreatePredictionEngine<InternalModelInput, InternalModelOutput>(model);
            return engine.Predict(input);
        }
    }

    private static InternalModelInput MapToModelInput(EscalationPredictionFeatures f)
    {
        return new InternalModelInput
        {
            SeverityScore = f.SeverityScore,
            PriorityScore = f.PriorityScore,
            PeopleAffected = f.PeopleAffected,
            VehiclesAffected = f.VehiclesAffected,
            Casualties = f.Casualties,
            ImmediateDanger = f.ImmediateDanger,
            PeopleTrapped = f.PeopleTrapped,
            CriticalInfrastructureAffected = f.CriticalInfrastructureAffected,
            InfrastructureDamage = f.InfrastructureDamage,
            RoadBlocked = f.RoadBlocked,
            EscalationCount = f.EscalationCount,
            DuplicateCount = f.DuplicateCount,
            RelatedIncidentCount = f.RelatedIncidentCount,
            ResourceShortage = f.ResourceShortage,
            IncidentAgeMinutes = f.IncidentAgeMinutes
        };
    }

    private static IReadOnlyDictionary<string, double> BuildSignalsDictionary(EscalationPredictionFeatures f)
    {
        return new Dictionary<string, double>
        {
            ["Severity Score"] = f.SeverityScore,
            ["Priority Score"] = f.PriorityScore,
            ["People Affected"] = f.PeopleAffected,
            ["Vehicles Affected"] = f.VehiclesAffected,
            ["Casualties"] = f.Casualties,
            ["Immediate Danger"] = f.ImmediateDanger,
            ["People Trapped"] = f.PeopleTrapped,
            ["Critical Infrastructure"] = f.CriticalInfrastructureAffected,
            ["Infrastructure Damage"] = f.InfrastructureDamage,
            ["Road Blocked"] = f.RoadBlocked,
            ["Escalation Count"] = f.EscalationCount,
            ["Duplicate Count"] = f.DuplicateCount,
            ["Related Incidents"] = f.RelatedIncidentCount,
            ["Resource Shortage"] = f.ResourceShortage,
            ["Incident Age (Min)"] = Math.Round(f.IncidentAgeMinutes, 1)
        };
    }

    private static string BuildFeatureProvenance(
        PredictionFeatureSnapshot snapshot)
        => JsonSerializer.Serialize(new
        {
            snapshot.SnapshotAtUtc,
            environmentalContextAvailable =
                snapshot.Features.EnvironmentalContextAvailable,
            featureSchema = snapshot.Features.EnvironmentalContextAvailable
                ? FeatureSchemaVersions.EscalationRiskV2
                : FeatureSchemaVersions.EscalationRiskV1
        });

    private sealed class InternalModelInput
    {
        [LoadColumn(0)] public float SeverityScore { get; set; }
        [LoadColumn(1)] public float PriorityScore { get; set; }
        [LoadColumn(2)] public float PeopleAffected { get; set; }
        [LoadColumn(3)] public float VehiclesAffected { get; set; }
        [LoadColumn(4)] public float Casualties { get; set; }
        [LoadColumn(5)] public float ImmediateDanger { get; set; }
        [LoadColumn(6)] public float PeopleTrapped { get; set; }
        [LoadColumn(7)] public float CriticalInfrastructureAffected { get; set; }
        [LoadColumn(8)] public float InfrastructureDamage { get; set; }
        [LoadColumn(9)] public float RoadBlocked { get; set; }
        [LoadColumn(10)] public float EscalationCount { get; set; }
        [LoadColumn(11)] public float DuplicateCount { get; set; }
        [LoadColumn(12)] public float RelatedIncidentCount { get; set; }
        [LoadColumn(13)] public float ResourceShortage { get; set; }
        [LoadColumn(14)] public float IncidentAgeMinutes { get; set; }
        [LoadColumn(15)] public bool Label { get; set; }
    }

    private sealed class InternalModelOutput
    {
        public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}

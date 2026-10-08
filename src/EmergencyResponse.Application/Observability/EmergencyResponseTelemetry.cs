using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EmergencyResponse.Application.Observability;

public static class EmergencyResponseTelemetry
{
    public const string ActivitySourceName = "EmergencyResponse.Application";
    public const string MeterName = "EmergencyResponse.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> IncidentsCreated =
        Meter.CreateCounter<long>("emergency.incidents.created");

    public static readonly Counter<long> AIAnalysesCompleted =
        Meter.CreateCounter<long>("emergency.ai.analyses.completed");

    public static readonly Counter<long> AIAnalysesFailed =
        Meter.CreateCounter<long>("emergency.ai.analyses.failed");

    public static readonly Counter<long> DuplicateDetections =
        Meter.CreateCounter<long>("emergency.duplicates.detected");

    public static readonly Counter<long> SituationsCreated =
        Meter.CreateCounter<long>("emergency.situations.created");

    public static readonly Counter<long> EscalationsOpened =
        Meter.CreateCounter<long>("emergency.escalations.opened");

    public static readonly Counter<long> NotificationsCreated =
        Meter.CreateCounter<long>("emergency.notifications.created");

    public static readonly Counter<long> AIJobsEnqueued =
        Meter.CreateCounter<long>("emergency.ai.jobs.enqueued");

    public static readonly Counter<long> AIJobsProcessed =
        Meter.CreateCounter<long>("emergency.ai.jobs.processed");

    public static readonly UpDownCounter<long> AIQueueLength =
        Meter.CreateUpDownCounter<long>("emergency.ai.queue.length");

    public static readonly Histogram<double> AIAnalysisDuration =
        Meter.CreateHistogram<double>("emergency.ai.analysis.duration", unit: "ms");

    public static readonly Histogram<double> DuplicateDetectionDuration =
        Meter.CreateHistogram<double>("emergency.duplicate.detection.duration", unit: "ms");

    public static readonly Histogram<double> VisualAnalysisDuration =
        Meter.CreateHistogram<double>("emergency.visual.analysis.duration", unit: "ms");

    public static readonly Counter<long> RecoveryAttempts = Meter.CreateCounter<long>("recovery_attempt_total");
    public static readonly Counter<long> RecoverySuccesses = Meter.CreateCounter<long>("recovery_success_total");
    public static readonly Counter<long> RecoveryFailures = Meter.CreateCounter<long>("recovery_failure_total");
    public static readonly Counter<long> QueueRedeliveries = Meter.CreateCounter<long>("queue_redelivery_total");
    public static readonly Counter<long> QueueDeadLetters = Meter.CreateCounter<long>("queue_deadletter_total");
    public static readonly Counter<long> WorkerFaults = Meter.CreateCounter<long>("worker_fault_total");
    public static readonly Counter<long> DependencyCircuitOpen = Meter.CreateCounter<long>("dependency_circuit_open_total");
    public static readonly Histogram<double> AIDegradedDuration = Meter.CreateHistogram<double>("ai_degraded_duration", unit: "s");
    public static readonly Histogram<double> DatabaseRestoreDuration = Meter.CreateHistogram<double>("database_restore_duration", unit: "s");
    public static readonly Histogram<double> QueueAge = Meter.CreateHistogram<double>("queue_age", unit: "s");
    public static readonly Histogram<double> WorkerLag = Meter.CreateHistogram<double>("worker_lag", unit: "s");

    public static readonly Counter<long> PredictionsCompleted =
        Meter.CreateCounter<long>("emergency.predictions.completed");

    public static readonly Counter<long> PredictionsFailed =
        Meter.CreateCounter<long>("emergency.predictions.failed");

    public static readonly Counter<long> PredictionJobsEnqueued =
        Meter.CreateCounter<long>("emergency.prediction.jobs.enqueued");

    public static readonly Counter<long> PredictionJobsProcessed =
        Meter.CreateCounter<long>("emergency.prediction.jobs.processed");

    public static readonly UpDownCounter<long> PredictionQueueLength =
        Meter.CreateUpDownCounter<long>("emergency.prediction.queue.length");

    public static readonly Histogram<double> PredictionDuration =
        Meter.CreateHistogram<double>("emergency.prediction.duration", unit: "ms");

    public static readonly Counter<long> CopilotToolCalls =
        Meter.CreateCounter<long>("emergency.copilot.tool.calls");

    public static readonly Counter<long> CopilotToolFailures =
        Meter.CreateCounter<long>("emergency.copilot.tool.failures");

    public static readonly Histogram<double> CopilotToolDuration =
        Meter.CreateHistogram<double>("emergency.copilot.tool.duration", unit: "ms");

    public static readonly Counter<long> CopilotActionsProposed =
        Meter.CreateCounter<long>("emergency.copilot.actions.proposed");

    public static readonly Counter<long> CopilotActionsApproved =
        Meter.CreateCounter<long>("emergency.copilot.actions.approved");

    public static readonly Counter<long> CopilotActionsRejected =
        Meter.CreateCounter<long>("emergency.copilot.actions.rejected");

    public static readonly Counter<long> CopilotActionsExpired =
        Meter.CreateCounter<long>("emergency.copilot.actions.expired");

    public static readonly Counter<long> CopilotActionsExecuted =
        Meter.CreateCounter<long>("emergency.copilot.actions.executed");

    public static readonly Counter<long> CopilotActionFailures =
        Meter.CreateCounter<long>("emergency.copilot.actions.failed");

    public static readonly Counter<long> AIPromptAttacksDetected =
        Meter.CreateCounter<long>("emergency.ai.security.prompt_attacks");

    public static readonly Counter<long> AIDocumentAttacksDetected =
        Meter.CreateCounter<long>("emergency.ai.security.document_attacks");

    public static readonly Counter<long> AIOutputSecurityBlocks =
        Meter.CreateCounter<long>("emergency.ai.security.output_blocks");

    public static readonly Counter<long> AISensitiveDataDetections =
        Meter.CreateCounter<long>("emergency.ai.security.sensitive_data");

    public static readonly Counter<long> AIGroundingValidationFailures =
        Meter.CreateCounter<long>("emergency.ai.security.grounding_failures");
}

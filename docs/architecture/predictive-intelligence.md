# Predictive Intelligence

Predictive Intelligence provides advisory escalation-risk signals for operators. The
first model estimates whether an incident is likely to require a qualifying escalation
within the next six hours.

## Operational boundary

Predictions never change incident severity, priority, dispatch orders, escalation
state, or any other deterministic operational decision. The API and Web client label
the result as advisory and require human review.

## Training and serving

Training is isolated in `ml/EmergencyResponse.PredictiveTraining`. It generates
historical incident snapshots, labels each snapshot from the observed outcome in the
following six-hour window, evaluates an ML.NET binary classifier, and writes the
versioned `escalation-risk-v1.zip` artifact.

The production API only loads the immutable artifact configured by
`Predictive:ModelPath`. Missing or invalid artifacts produce a persisted failed
prediction and do not fail incident processing.

## Training data

Training rows come from point-in-time `IncidentPredictionSnapshot` records. A
snapshot represents the feature state captured when the prediction-time observation
was made. Future escalation records are used only to generate labels; they are not
features for the earlier snapshot.

The current incident table contains latest-state values for several operational
fields and cannot reconstruct their historical values safely. Those fields are
therefore not backfilled from today's row. The runtime captures complete snapshots
for future training, and the exporter rejects datasets without sufficient provenance.

The label is `true` when a qualifying escalation occurs in
`(SnapshotAtUtc, SnapshotAtUtc + 6 hours]`. Snapshots newer than the data cutoff
minus six hours are excluded because their future windows are incomplete.

## Temporal evaluation

Rows are ordered by `SnapshotAtUtc`: 70% oldest for training, 15% for validation,
and 15% newest for final testing. Candidate models are selected using validation
metrics only. The test set is evaluated once after selection and is never randomly
shuffled into the training data.

Reports include precision, recall, F1, AUC, confusion-matrix counts, and an
always-negative baseline so false negatives remain visible.

## Versioning

Datasets, feature schemas, and model artifacts are versioned independently. A
published model contains:

- `model.zip`
- `manifest.json`

under `ml/Models/escalation-risk/<version>`. `current.json` points to the active
version without overwriting older artifacts. Each production `PredictiveAnalysis`
stores model, dataset, and feature-schema versions for audit and rollback.

## Features and leakage prevention

The serving feature builder uses only records whose timestamps are less than or equal
to the snapshot time. The canonical feature set contains severity, priority, affected
people and vehicles, casualties, operational danger indicators, escalation and
duplicate counts, related-incident counts, resource shortage, and incident age.
Future escalation outcomes are labels for training only and are never serving
features.

Each prediction stores the serialized `PredictionFeatureSnapshot`, including the
incident ID, snapshot timestamp, canonical features, and SHA-256 feature fingerprint.
The background worker compares this fingerprint with the latest prediction and skips
redundant inference when the operational signals are unchanged.

## Runtime flow

AI processing enqueues a predictive job after analysis and deterministic evaluation
complete. A bounded in-memory queue feeds the hosted worker. The worker builds a
snapshot, suppresses duplicate inference, evaluates the model, persists the result,
and broadcasts `PredictionUpdated` through SignalR. Operators can also enqueue a
manual re-evaluation through the incident API.
## Phase 13 Part 3: Operational forecasting

Operational forecasting is advisory and is separate from deterministic incident,
escalation, and resource-command rules. It currently exposes three signals:

- `INCIDENT_VOLUME:ALL` uses regular hourly buckets and ML.NET SSA when at least
  336 observations and 20 non-zero observations are available.
- `RESOURCE_DEMAND:<ResourceType>` expands incident-type preferred requirements
  at incident creation time, forecasts the resulting hourly demand series, and
  compares peak demand with resources whose status is `Available`.
- Geographic hotspots use deterministic coordinate grid cells. Recent activity,
  historical activity growth, critical/high counts, and affected people are
  normalized into an explainable ranking score. The grid is not a hazard
  boundary and does not trigger an operational command.

Forecasts are retained in `OperationalForecasts`; completed periods can be
evaluated through `ForecastEvaluations`. A hosted worker refreshes forecasts
hourly, while authorized callers can request incident-volume regeneration. The
API returns an unavailable result when history is insufficient rather than
inventing values. Forecast notifications are lower priority than deterministic
critical emergency alerts and are phrased as potential shortages or advisory
hotspot changes.

No forecast may open an escalation, dispatch a resource, change severity or
priority, or initiate evacuation. Operators remain responsible for reviewing
forecast uncertainty and making operational decisions.

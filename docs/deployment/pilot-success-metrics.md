# Pilot Success Metrics

Status: Proposed engineering acceptance targets; no observed pilot results. Apply to the approved synthetic cohort and exact build/dataset recorded in the [go/no-go evidence register](pilot-go-no-go.md#evidence-register).

## Measurement contract

Record test/run ID, build SHA and image digest, dataset version, environment, start/end time, scenario, attempts, valid outcomes, failures, evidence link and reviewer. A mandatory case that is blocked or unexecuted remains pending and cannot pass. Denials/conflicts are successful expected outcomes only in cases designed to test those controls.

| Metric | Calculation and initial target | Required evidence |
|---|---|---|
| Critical incident create/retrieve | Successful expected outcomes / agreed mandatory cases = 100% | API result, persisted identifier, authorized retrieval, ownership/scope denials |
| Unauthorized operational execution | Count = 0 | Attempts by Citizen, Responder and unauthorized Operator; verify absence of business changes |
| Approval bypass | Count = 0 | Conversation-only approval, wrong role, missing authentication, stale/rejected/expired proposal |
| Duplicate side effects | Count = 0 after retry/redelivery/replay | Job/action identifiers, state/history/audit; one intended effect |
| Critical audit completeness | Cases with all required actor/action/target/time/result/correlation evidence / mandatory cases = 100% | SystemAdmin review; proposal, approval and execution linked without secrets |
| AI safety | Every mandatory case passes independently | Injection, output safety, scope and abstention artifacts; averages cannot offset a failed critical case |
| Citation correctness | ≥95% of evaluated citations valid and supported | Versioned evaluation results and permitted source verification |
| Groundedness | ≥90% under established evaluation method | Reviewed answer/evidence cases; unsafe case still fails independently |
| Retrieval Recall@5 | ≥90% | Approved corpus and retrieval evaluation |
| Indirect attack detection | 100% of mandatory attack cases | Checked-in evaluation cases and reviewed artifact |
| Core workflow during AI outage | All agreed incident cases persist/retrieve; zero false AI-success claims | Outage record, job retained/retryable outcome, UI state and recovery check |
| Resource concurrency | All competing-write cases reject/reconcile conflicts; zero silent overwrites | Two accounts/contexts; current assignment, row version and histories |
| Recovery | SQL/Blob restore and durable queue recovery drills pass; target RTO ≤60 min, RPO ≤15 min | Actual drill timestamps, integrity/media checks, data-loss estimate |
| Operator usability | Each role completes every agreed permitted scenario without unauthorized or undocumented workaround | Observer record, elapsed time, participants' feedback and corrective actions |
| Application responsiveness | Existing subsystem P95 gates and API errors <1%; relative latency increase >20% requires review | Staging baseline and comparable run, P50/P95/P99 and sample counts |

AI thresholds derive from `tests/EmergencyResponse.CopilotEvaluation/EvaluationThresholds.json`; preserve all its required gates, including incident/visual critical recall and tool selection. See [AI evaluation](../testing/ai-evaluation.md) and [performance gates](../testing/performance-gates.md). Do not average distinct safety measures into a release score.

## Recovery and availability

RTO = verified service restoration time minus outage start; also record detection and recovery-start times. RPO = outage time minus newest reliably recovered required state, with an explicit data-loss estimate. Use [recovery objectives](recovery-objectives.md); these targets are unproven until a drill measures them.

Availability = successful scheduled probes / valid scheduled probes, with downtime duration and failures retained. Sample Web/API public HTTPS health paths every minute from an approved monitor and private SQL paths from the connected environment. Probe success alone does not establish functional correctness. The pilot owner must approve baseline-derived service targets before entry; implementation and alert delivery remain pending under [monitoring](monitoring-and-alerts.md).

Measure model latency, token counts, throttling and cost separately from application correctness. Run controlled low-volume real-provider evaluation only with approved cost/data boundaries.

## Review register

| Review point | Required review | Actual result/evidence | Owner/date |
|---|---|---|---|
| Before entry | Mandatory tests, baseline, recovery, alerts, tabletop and governance | Pending | Pending |
| After each session | Failed cases, safety events, stale data, usability and open actions | Not conducted | Pending |
| Week-one review | Trend/threshold review and corrective-action retests | Not conducted | Pending |
| Week-two close | All evidence, retained defects, disposal plan and expansion decision | Not conducted | Pending |

Every defect records severity, affected capability, owner, due date, corrective change and retest evidence. Zero tolerance safety failures trigger the [stop procedure](pilot-go-no-go.md#stop-and-resumption-authority) immediately.

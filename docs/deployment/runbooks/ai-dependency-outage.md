# AI, Search and External Context Outage Runbook

Owner: Technical support lead; operations lead coordinates fallback. Status: Draft; staged failure/recovery checks pending.

## 1. Symptoms

Analysis remains pending/failed, provider calls time out/return 429 or 5xx, a circuit opens, Copilot lacks retrieved evidence, weather/map context is unavailable, or results are stale. Configuration readiness alone does not prove a model call succeeds.

## 2. Impact and severity

AI/Search/context failures degrade assistance. Incident persistence may continue when SQL and required configuration are healthy. Unsupported advice presented as verified fact is a safety failure; stop that workflow. Missing map/media can prevent dependent review even when intake works.

## 3. How to confirm the issue

Check incident/job execution status, timestamp, failure category, retry state and sanitized traces. Compare provider status/rate limits and Search access/index readiness. Confirm core incident create/retrieve using synthetic records. Check freshness where available; if freshness cannot be distinguished, treat data as untrusted/stale and record a blocker.

## 4. Immediate containment

Suspend affected AI/Copilot/context decisions; tell operators to verify facts manually. Preserve incidents, media and durable jobs. Avoid retry storms or large real-model tests. Search outage requires clear unavailable/insufficient-evidence response; no fabricated citations, weather or model success.

## 5. Authorized recovery steps

Support verifies endpoint/deployment, identity roles, quota, network and provider health using approved configuration. Restore the dependency or correct the reviewed configuration through the deployment process. Allow configured backoff/circuit behavior to recover. Reprocess a small permitted sample with an authorized application's control only after checking existing job/state; no blind mass replay. Use [queue backlog](queue-backlog.md) for durable work recovery.

## 6. Verification after recovery

Demonstrate incident persistence during outage; demonstrate AI status changes truthfully and retained jobs complete/retry safely after recovery. Confirm Copilot uses authorized evidence/citations and abstains when evidence is absent. Check failed/stale assessments remain identifiable, duplicate effects are zero, and provider load stays bounded.

## 7. Escalation contacts

Support lead → operations lead → pilot owner; security reviewer for unsafe/ungrounded output or data leakage; provider support through the approved organizational channel. Assign named contacts in [pilot scope](../pilot-scope.md).

## 8. Audit and evidence requirements

Record provider/deployment version, failure category, correlation/job IDs, outage/detection/recovery times, retries, rate-limit counts, sample results and approvals. Never include API keys, tokens, raw prompts, model responses or sensitive payloads. Link restricted evaluation artifacts where needed.

## 9. Conditions for returning to normal operation

Dependency verified, affected workflows pass mandatory samples, backlog is recovering within approved bounds and operators can distinguish pending/failed/current results. Operations lead authorizes routine recovery; formal suspension requires [pilot-owner approval](../pilot-go-no-go.md#stop-and-resumption-authority) and relevant security sign-off.

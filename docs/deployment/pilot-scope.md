# Pilot Scope

Status: **DRAFT — NO-GO**. Prepared 2026-10-14. This is a proposed synthetic rehearsal; entry requires the [go/no-go decision](pilot-go-no-go.md). The [staging report](staging-verification.md) records the latest known deployment status; cloud state must be checked again before entry.

## Purpose and boundaries

The platform supports human incident review and coordination. AI recommendations are advisory. The rehearsal cannot dispatch actual responders, connect to official emergency systems, or authorize real emergency operations. Use fabricated descriptions, test coordinates, synthetic media, and approved synthetic knowledge sources in an isolated test environment. Participants follow organizational procedures for any actual emergency outside the rehearsal.

Proposed duration: two weeks from the approved start date, with one two-hour session each weekday in Asia/Calcutta time. Exact dates and session hours are pending owner approval. Maximum six individually assigned test accounts; no shared credentials. Start with a 45–60 minute tabletop before application sessions. Review results after each session, at the end of week one, and at the end of week two. Expansion requires a new documented decision.

| Capability | Proposed boundary | Evidence needed before inclusion | Current eligibility |
|---|---|---|---|
| Incident reporting | Citizen creates fabricated reports; retrieval remains ownership/scope restricted | Persistence, validation, Citizen ownership and Operator scope tests | Pending staging verification |
| Incident triage | Operator reviews facts, severity, priority, confidence and authorized corrections | Incident lifecycle, incorrect AI output handling, state history | Pending staging verification |
| Incident map | Authorized views only; verify coordinate accuracy and distinguish unavailable layers | Map authorization, plotted location, stale/unavailable state tests | Pending staging verification |
| Resource coordination | Operator assignment; EmergencyAdmin/SystemAdmin dispatch commands; all resources simulated | Permission checks, valid transitions, conflict handling, audit | Pending staging verification |
| Escalations and situations | Operator review/acknowledgment; authorized administrator resolution | Access scope, acknowledgment/resolution, linked incident state and audit | Pending staging verification |
| Knowledge Copilot | Approved synthetic sources, citations checked by participants | Retrieval, citation, grounding, abstention and document access evaluation | Pending staging verification |
| Controlled actions | Proposal by permitted user; explicit authenticated approval by EmergencyAdmin/SystemAdmin; server revalidation | Denial, stale proposal, replay, approval and execution audit tests | Pending staging verification |
| Predictions | Advisory display only; never an instruction to dispatch | Reviewed prediction inputs, uncertainty and no autonomous command tests | Pending staging verification; exclude if restart safety unproven |
| External context | Show source/time or clearly unavailable/stale | Weather/maps degradation and freshness tests | Pending staging verification |
| Notifications | Correct test recipients; repeat delivery never repeats business actions | Recipient scope, inbox, deduplication, redelivery tests | Pending staging verification |

A capability enters the cohort only after the owner records its exact passing evidence and changes its eligibility. Excluding a capability requires an explicit participant instruction and verified access/configuration restriction; a document label alone does not enforce exclusion.

## Participants and accountability

| Test account role | Count | Exercise responsibility | Named participant |
|---|---:|---|---|
| Citizen | 1 | Submit synthetic incidents; ownership-denial checks | Pending |
| Operator | 2 | Triage, assignment conflict, propose actions, acknowledge and hand over | Pending each |
| Responder | 1 | Read incidents where authorized; test restricted access | Pending |
| EmergencyAdmin | 1 | Authorized simulated dispatch, resolution and action approval | Pending |
| SystemAdmin | 1 | Test administration, audit review and controlled recovery | Pending |

The authorization map grants Responder resource-read permission, but the current resource controller restricts routes to Operator/EmergencyAdmin/SystemAdmin. Treat Responder resource screens as unverified and excluded until the actual endpoint contract is resolved and tested. Never grant an extra role to bypass this discrepancy.

| Accountable function | Responsibility | Name/contact/backup |
|---|---|---|
| Pilot owner | Scope, schedule, go/no-go, whole-pilot suspension and resumption | Pending |
| Operations lead | Facilitation, shift handover, support coordination, workflow suspension | Pending |
| Security reviewer | Access/data review, security incidents, required security sign-off | Pending |
| Technical support lead | Monitoring, deployments, restoration and evidence collection | Pending |
| Data/organization owner | Synthetic inputs, retention, provider use and governance approval | Pending |
| Exercise observer | Independent expected/actual record and corrective action tracking | Pending |

These functions may overlap after approval, but approval and technical verification must both be recorded. All functions, participant names, contact channel, response expectations and an escalation backup must be assigned before entry. Support is staffed for the full two-hour session; if it is unavailable, postpone or stop the session. Outside sessions, participants submit no new rehearsal work.

## Browsers and known limitations

Proposed browsers: current stable desktop Chrome and Edge on Windows. Record tested versions in the evidence register; mobile and other browsers are excluded until tested.

Latest known limitations from the staging record: no Azure staging deployment or functional verification is evidenced; Container Apps environment quota/deletion was blocking provisioning. Dedicated Worker host/deployment is deferred. The proposed API uses one replica; several situation, knowledge and predictive job paths remain process-local. Queue age/worker fault instruments are declared without observed runtime recording/export. Independent consumer scaling, zero-loss process-local restart recovery, and full observability are unproven.

The [full pilot-entry criteria](pilot-entry-criteria.md) still require durable processing and independent Worker readiness. This synthetic rehearsal does not satisfy those requirements. Any bounded rehearsal exception must be signed by the owner and security reviewer, identify excluded workflows, and prove the included durable paths recover. Never waive authorization, persistence correctness, audit or recovery of required state.

## Data governance record

| Review | Basis | Status | Reviewer/date/evidence |
|---|---|---|---|
| Classification and scope | [Data classification](../security/data-classification.md); synthetic inputs only | Pending | Pending |
| Access and minimization | [Privacy](../security/privacy.md); isolated identities and role/scope tests | Pending | Pending |
| Retention and disposal | [Retention policy](../security/retention-policy.md); engineering defaults need owner approval | Pending | Pending |
| AI/provider processing | Synthetic data may leave the application for selected providers; approved region and account required | Pending | Pending |
| Organizational/legal requirements | Responsible organization reviews applicable requirements and participant notice | Pending | Pending |
| Test evidence handling | Restricted artifact access; identifiers/timestamps, no credentials or sensitive payloads | Pending | Pending |

Do not invent legal retention periods or purge records automatically at rehearsal end. The owner approves retention for test incidents, media, knowledge, logs, backups and evidence; technical support records authorized disposal or continued retention. Real data requires a separate governance decision and expanded safety/operational approval.

## Rollout and stop

Staging verification → tabletop → approved small synthetic cohort → daily evidence review → corrections/retests → formal expansion decision. Use the [operator guide](operator-guide.md), [monitoring requirements](monitoring-and-alerts.md), and [stop/resumption rules](pilot-go-no-go.md#stop-and-resumption-authority). Proposed dates, pending owners or unchecked evidence mean NO-GO.

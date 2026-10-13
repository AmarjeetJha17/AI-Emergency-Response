# Pilot Go/No-Go Decision

Status: **NO-GO**. Prepared 2026-10-14; formal decision review not conducted. Decision owner, decision date, approved start/end dates and next review date: pending assignment. This record governs the proposed [two-week synthetic rehearsal](pilot-scope.md); real operational use requires separate organizational approval.

## Technical readiness

- [ ] Exact release SHA/images/digests, dataset and environment are identified; [staging verification](staging-verification.md) reviewed.
- [ ] Mandatory build, SQL integration, ownership/scope authorization and AI safety/evaluation cases pass for that release.
- [ ] Private SQL/Blob access, runtime identities, ACR pulls, Key Vault references, HTTPS and CORS verified.
- [ ] Incident creation/retrieval and resource/escalation/situation workflows pass with complete audit evidence.
- [ ] SQL/Blob restoration and durable queue restart/redelivery recovery demonstrated with measured RTO/RPO.
- [ ] AI/Search/context failure behavior and browser SignalR reconnect/snapshot recovery demonstrated.
- [ ] Required telemetry records data; monitoring rules and alert notification/acknowledgment tested.
- [ ] Rollback/forward recovery reviewed for application and schema compatibility.
- [ ] Process-local queue limitations and missing standalone Worker resolved for full pilot, or a bounded synthetic-rehearsal exclusion/exception documented, enforced and signed without waiving required-state recovery.

## Operational readiness

- [ ] [Operator guide](operator-guide.md) and all eight [runbooks](runbooks/incident-triage.md) reviewed/approved.
- [ ] [Tabletop](tabletop-exercise.md) conducted; actual results and critical retests recorded.
- [ ] Every included capability has passing staging evidence and demonstrated degraded behavior.
- [ ] Named participants, individually assigned accounts, owners, support channel and backups recorded.
- [ ] Shift handover rehearsed; support available throughout each approved session.
- [ ] [Metrics](pilot-success-metrics.md), baseline, review cadence and actionable thresholds agreed.

### Runbook inventory

| Runbook | Primary accountable function |
|---|---|
| [Incident triage](runbooks/incident-triage.md) | Operations lead |
| [AI, Search and context outage](runbooks/ai-dependency-outage.md) | Technical support lead |
| [Database outage](runbooks/database-outage.md) | Technical support/SystemAdmin |
| [Queue backlog](runbooks/queue-backlog.md) | Technical support/SystemAdmin |
| [Security incident](runbooks/security-incident.md) | Security reviewer |
| [Failed deployment](runbooks/failed-deployment.md) | Authorized deployment administrator |
| [Restore and recovery](runbooks/restore-and-recovery.md) | SystemAdmin/authorized database administrator |
| [Shift handover](runbooks/shift-handover.md) | Operations lead |

## Data governance and pilot controls

- [ ] Scope/retention/provider/evidence reviews in [pilot scope](pilot-scope.md) signed by responsible owners.
- [ ] Synthetic versus real data boundary enforced; actual dispatch/integration inaccessible in rehearsal.
- [ ] Participant notice, applicable privacy/legal/organization requirements reviewed.
- [ ] Duration, dates, hours, cohort and tested Chrome/Edge versions confirmed.
- [ ] Exclusions and known defects disclosed; no undocumented operational workarounds.
- [ ] Stop authority, communication, recovery verification and resumption procedure acknowledged.
- [ ] Authorized owner approves entry after all mandatory evidence and reviewer sign-offs.

## Stop and resumption authority

Any participant may stop their own affected activity and immediately notify the operations lead. The lead suspends the affected workflow and escalates to the pilot owner. The owner may suspend the entire rehearsal; the security reviewer may require immediate security suspension. If the owner cannot be reached, the operations lead keeps the pilot suspended. Notify technical support and the security reviewer; notify the data/organization owner for exposure, data loss or governance violations.

Immediately suspend the affected workflow for any unauthorized high-impact action, out-of-scope exposure, missing/inconsistent critical audit, false incident-save claim, silent resource overwrite, unrecoverable required state, repeated unvalidated AI claims presented as fact, or inability to distinguish stale/degraded state. Suspend the entire rehearsal if the scope of impact is unknown, a shared authorization/persistence/audit control is compromised, or isolation of the affected workflow cannot be verified. This does not wait for a latency/error alert.

Containment is procedural and uses approved existing administrative controls; there is no documented application-wide pilot-stop switch. Participants stop issuing commands. SystemAdmin/authorized deployment support preserves evidence and uses reviewed access or revision restrictions if required. Do not delete evidence, weaken permissions, grant runtime db_owner, expose private services or replay unknown jobs.

Resumption requires root cause/impact review, preserved evidence, correction, affected mandatory tests, authoritative state and audit reconciliation, and recovery verification. The technical support lead records verification; the security reviewer signs security/safety corrections and the data owner signs exposure/loss findings. Only the named pilot owner records permission to resume, scope, conditions and review time. If any required approval/evidence is missing, status remains NO-GO/suspended.

## Evidence register

Entries describe the latest recorded evidence, not a live claim about Azure. Update only after a real check; include build/run/deployment links, timestamps and reviewer.

| ID | Check | Current evidence/status | Owner/reviewer and corrective action |
|---|---|---|---|
| E01 | Release/source | Current staging-preparation checkpoint is commit `83f4817`; versioned Azure image digests and migration execution remain pending | Technical support; bind deployed images and test evidence to exact source |
| E02 | Staging foundation/apps | Latest report: quota blocked, old environment ScheduledForDelete; no staging deployment evidenced | Technical support; refresh preview and complete Part 4 |
| E03 | Local packaging | Latest report: Release build, Bicep, Web/API Docker builds, Web liveness and Linux tools prepared | Technical support; attach logs/version; local checks do not pass staging |
| E04 | Private identities/network/media/auth | Definitions present; runtime checks pending | Support + security; record positive/negative access evidence |
| E05 | Mandatory workflow/security/AI cases | Pilot release evidence not linked | Support + security; record CI/integration/evaluation and staged scenarios |
| E06 | SQL/Blob restore and queue recovery | Actual staging drills not recorded | SystemAdmin/support; measure recovery and duplicate effects |
| E07 | Telemetry and alerts | Declarations present; runtime queue/fault recording and export/notification pending | Support; implement/verify missing signals separately |
| E08 | Tabletop/operator usability | Planned, not conducted | Operations lead + observer; record actual inject results/retests |
| E09 | Governance | Engineering policies exist; organizational review pending | Security + data/organization owner |
| E10 | Ownership/cohort/browsers | Roles/default cohort proposed; names, contacts, dates and tested versions pending | Pilot owner |
| E11 | Worker/process-local limitations | Dedicated host deferred, API single replica, some channels process-local | Support + owner; resolve full pilot requirement or bounded approved exclusions |
| E12 | Documentation structure | 2026-10-14: six new pilot documents and eight runbooks prepared; relative file links and all nine ordered runbook sections validated; whitespace check passed | Documentation validation only; operational reviews and approval still pending |

New observations append/update evidence with date, reviewer and linked artifacts. Every failure tracks severity, capability, remediation owner/due date, retest and closure. A pending/missing mandatory item cannot be silently marked not applicable.

## Decision and sign-offs

| Decision field | Recorded value |
|---|---|
| Decision | NO-GO — evidence and ownership incomplete |
| Decision owner/name/contact | Pending |
| Formal decision date/time | Pending |
| Unresolved blockers | E01–E02 and E04–E11; current staging and operational evidence absent |
| Conditions of approval | All mandatory gates passed, enforced scope, explicit reviewer sign-offs |
| Next review date | Pending; schedule after staging blocker resolution |
| Expansion | Not authorized; requires new evidence review and scope/governance decision |

| Approval | Name/date/evidence | Status |
|---|---|---|
| Technical verification | Pending | Not approved |
| Operator procedures and exercise | Pending | Not approved |
| Security review | Pending | Not approved |
| Data/organization governance | Pending | Not approved |
| Pilot owner entry decision | Pending | Not approved |

Use [pilot-entry criteria](pilot-entry-criteria.md) for full deployment readiness and [release checklist](release-checklist.md) for software release checks. These documents establish requirements; checklists alone cannot authorize entry without actual evidence.

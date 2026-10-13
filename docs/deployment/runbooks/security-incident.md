# Security Incident Runbook

Owner: Security reviewer; technical support/SystemAdmin performs containment. Status: Draft; tabletop and response approval pending.

## 1. Symptoms

Unauthorized action/exposure, approval bypass, unexpected privileges, suspicious auth activity, prompt/document instructions bypassing policy, leaked credentials, missing critical audit or altered operational records.

## 2. Impact and severity

Authorization bypass, out-of-scope data exposure, unintended dispatch or compromised shared controls is critical even at low request volume. Failed unauthorized attempts and intended negative-test denials require review but are not themselves evidence of executed harm.

## 3. How to confirm the issue

Correlate actor role/scope, route/operation, proposal/approval/job IDs, actual database effects and SystemAdmin audit. Inspect sanitized security/auth logs and approved synthetic reproduction only. Confirm whether the action was denied, proposed, approved or executed; one counter alone cannot prove that lifecycle.

## 4. Immediate containment

Stop affected commands and preserve evidence. Notify security reviewer, operations lead and pilot owner; suspend the entire rehearsal if scope is unknown or shared controls are compromised. Authorized support may restrict access/traffic, rotate/revoke compromised credentials or isolate the affected revision through reviewed controls. Do not assume revocation immediately invalidates existing JWTs; verify token lifetime/containment. No evidence deletion or permission bypass.

## 5. Authorized recovery steps

Security reviewer directs investigation; SystemAdmin/support applies approved access/configuration/code correction. Review ownership/department scope, approval revalidation, session/token behavior, prompt boundaries and affected records. Repair operational state only through an approved reconciled recovery procedure; do not silently erase unauthorized events. Use data/organization owner for exposure handling and approved organizational notification obligations.

## 6. Verification after recovery

Retest denied roles, object scope, expired/stale proposals, replay/idempotency and authorized success. Verify no unintended state effects, complete actor/action/result audit, private media access and secret-free logs. Reconcile affected users/records and verify containment on existing sessions as well as new authentication.

## 7. Escalation contacts

Security reviewer + operations lead → pilot owner; data/organization owner for exposure; technical support for isolation/remediation. Organization-approved incident channel and names/backups in [pilot scope](../pilot-scope.md) are mandatory before entry. Do not send external disclosures without the responsible organization's authority.

## 8. Audit and evidence requirements

Preserve timestamps, incident/correlation/proposal/job IDs, actor/scope, state effects, containment decisions and restricted artifact locations. Record access to evidence and change history. Do not put credentials, full personal data, raw malicious document contents or confidential responses into shared reports.

## 9. Conditions for returning to normal operation

Impact bounded, required state/audit reconciled, corrected critical controls pass tests and security reviewer signs the findings. Data owner signs relevant exposure/loss findings; technical support records verification; only the pilot owner approves [resumption](../pilot-go-no-go.md#stop-and-resumption-authority). Unresolved critical findings mean NO-GO.

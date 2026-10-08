# Data Retention Policy

This document defines engineering behavior, not legal retention periods. The organization responsible for deployment must set retention to meet its jurisdiction, operational, records-management, and legal requirements before production use.

## Data-specific handling

| Data type | Initial handling |
| --- | --- |
| Audit records | Retain according to approved long-term security/records policy; never purge through the initial worker |
| Incident records | Follow approved operational and legal policy; archive before any approved purge |
| AI analysis | Retain with the incident lifecycle; no automatic deletion in the initial worker |
| Copilot conversations | Archive after the configured inactivity period; do not hard-delete automatically |
| Notifications | Hard-delete after the configured age or explicit expiry |
| Temporary upload/video artifacts | Delete after processing; do not retain extracted frames by default |
| Failed AI/processing artifacts | Engineering default is 7 days; automated purge awaits a defined artifact store and policy |
| Application logs and telemetry | Engineering default is 30 days; configure at the hosting/observability layer |

## Engineering defaults

`RetentionOptions` defaults are 90 days for Copilot conversations, 30 days for notifications, 30 days for application logs, and 7 days for failed processing artifacts. The cleanup interval defaults to 24 hours. These are configurable engineering defaults, not legal requirements.

The current background worker archives inactive Copilot conversations by setting their existing archive timestamp and deletes aged or explicitly expired notifications. Each non-empty cleanup run writes one audit event with aggregate counts only. It does not delete incidents, audit records, AI analyses, logs, or failed artifacts. A future lifecycle may use Active → Archived → Purged after an approved retention rule and recovery process exist.

## Recovery and review

Retention changes must be reviewed with the data owner, tested against boundary dates and related records, and paired with a recovery procedure where deletion is irreversible. Backup retention and deletion must be aligned so expired data is not retained indefinitely in recoverable copies beyond approved requirements.

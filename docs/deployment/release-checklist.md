# Release Verification Checklist

Automated checks provide evidence for a release decision; a green workflow does not prove the whole platform is production-ready. Complete staging verification, recovery drills, configuration review, and an authorized release decision for each release.

## Build and code quality

- [ ] Release restore and build pass on the `master` commit being released.
- [ ] Unit and SQL Server integration tests pass.
- [ ] Domain/Application line coverage is at least 70%.
- [ ] CodeQL has no unresolved critical findings; dependency review has no high-severity finding.
- [ ] CI reports have been reviewed for warnings and flaky or skipped checks.

## AI quality and safety

- [ ] Offline AI evaluation passes the checked-in dataset thresholds.
- [ ] Prompt-injection, grounding, citation, and controlled-action approval checks pass.
- [ ] AI-provider failure and degraded behavior have been verified.
- [ ] No unreviewed model, prompt, or evaluation-dataset regression remains.

## Database and operations

- [ ] EF reports no pending model changes; every intended schema change has a reviewed migration.
- [ ] Generate and inspect the deployment SQL script or migration bundle; test it against a staging copy before production.
- [ ] Backup and restore have been tested for the target environment.
- [ ] Health/readiness, durable queue recovery, worker recovery, and audit records have been verified.

## Staging verification

- [ ] API and Web smoke checks pass in staging.
- [ ] Authentication, roles, and authorization boundaries are verified.
- [ ] Media access/uploads, dashboard, map, and SignalR reconnect/snapshot recovery are verified.
- [ ] Logging, telemetry, alerts, and operational dashboards are available.
- [ ] Performance and chaos results are reviewed against the target environment; local TestServer measurements are not treated as production capacity.

## Release approval

- [ ] Known limitations and operational risks are documented.
- [ ] Rollback or forward-recovery plan is documented and understood.
- [ ] API and Web artifacts are identified by the exact `v*` tag and have been downloaded/inspected.
- [ ] Authorized release owner approves the release.

## Repository controls

- [ ] Protect `master` and require the CI and CodeQL checks before merge; select the exact check names shown by successful GitHub Actions runs.
- [ ] Require dependency review where the repository supports it; enable the dependency graph and CodeQL security scanning for private repositories as applicable to the GitHub plan.
- [ ] Block force pushes to `master`.

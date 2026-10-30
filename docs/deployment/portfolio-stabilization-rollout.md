# Portfolio stabilization rollout preparation — 30 October 2026

## Final source checkpoint status — 30 October 2026

**Execution remains blocked.** The working tree contains nine uncommitted source and
documentation changes. The local `origin/master` reference is an ancestor of the
current branch (`fix/portfolio-runtime-stabilization`, one commit ahead), but a
network fetch was unavailable in this environment. No changes were staged or
committed. The current task explicitly prohibits Git commits, so a clean source
checkpoint cannot be created here.

The previously recorded 29 October candidate tag and local digests below are
superseded: they were built before the current worker gates and are not eligible for
push or rollout. The 30 October dirty-tree candidate tag `portfolio-stab-20261030-b0092608`
is also not available in the current local Docker image store and was built from
uncommitted source. It must not be used. After a separately authorized clean source
commit, build both images from that exact commit using a new unique tag, record the
resulting local image IDs and manifest digests, then obtain separate ACR-push approval.
Do not infer or reuse a digest from the dirty-tree build.

The live Azure snapshot in this document is historical, not refreshed for this
checkpoint: Azure CLI reported that no account is logged in in the current execution
environment. Before any rollout, reauthenticate in the operator's approved Azure
context and rerun the read-only snapshot and comparison. No Azure resources were
queried or changed in this checkpoint.

Local validation for this checkpoint: the API Bicep entry point compiled; all three
PowerShell deployment/test scripts passed parser validation; the preview metadata
redaction test passed; and `git diff --check` passed. The API Release build failed
under the only installed SDK (`11.0.100-rc.2`) with NU1902/NU1903 package advisories,
but MSBuild reported zero errors and provided no actionable failure detail. The
focused .NET worker-registration test did not complete: it emitted the same package
advisories, stalled without a test summary, and was stopped. Treat .NET validation as
incomplete, not passed. The redaction scan's only match was the deliberate
`DO_NOT_EMIT` synthetic fixture in the preview helper test.

The API template now has the intended settings: `AI__Enabled=false`,
`Forecasting__BackgroundWorkerEnabled=false`,
`DataRetention__BackgroundWorkerEnabled=false`, `AzureMaps__Enabled=false`, and the
exact approved CORS origin. The API must be rolled out first and checked before Web.
Use only the targeted revision update commands in the section below; do not use the
full-resource Bicep projection because its What-If predicts deletion of the native
secret collection.

**Execution checkpoint: BLOCKED. No image push or Azure update was performed.**

Baseline: [release gap review](portfolio-release-gap-review.md), source commit
`65d9714ba50ab46a565d8799cb32fc67dbfa4552`, branch `fix/portfolio-runtime-stabilization`.
This document and the new preview helper are preparation artifacts, outside the image source archive.

## Verified live baseline

Subscription `Azure subscription 1`, Enabled, `0a05d5ff-7fef-4886-8e72-22a6cafd5567`.
Both apps and `cae-er-portfolio-demo` are in Central US, resource group
`rg-er-portfolio-demo-centralus`. Image registry `acerportfoliobxwiyolb.azurecr.io`
remains in `rg-er-portfolio-demo-eastus2`; no infrastructure moves are proposed.

| Setting | API | Web |
|---|---|---|
| App / container | `ca-er-api-portfolio` / `api` | `ca-er-web-portfolio` / `web` |
| Revision mode | Single | Single |
| Latest = ready = active revision | `ca-er-api-portfolio--native-261024150514` | `ca-er-web-portfolio--web4584310b07df` |
| Provisioning / revision | Succeeded / Provisioned, Healthy | Succeeded / Provisioned, Healthy |
| Traffic | latest revision, 100% | latest revision, 100% |
| CPU / memory | 1 / 2Gi | 0.5 / 1Gi |
| Scale | min 0, max 1 | min 0, max 1 |
| Managed identity / registry identity | `id-er-portfolio-api` | `id-er-portfolio-web` |
| Native secret names only | `jwt-signing-key`, `admin-seed-password` | `syncfusion-license-key` |
| Environment | Staging | PortfolioDemo |

Each exact identity has a direct `AcrPull` assignment at the East US 2 registry scope,
verified by principal ID (API `d706e311-95d6-467f-8634-90eb8a77f0b7`,
Web `4d7867d3-1dcc-4ff8-94d3-366d1a16f6ca`). No grants were changed.
Registry authentication has no username/password binding.

Both ingress settings are external HTTP, Auto transport, target port 8080. Probes
are identical: HTTP 8080, liveness `/health/live` period 30/failure 3; readiness
`/health/ready` period 10/failure 3; startup `/health/live` initial delay 10,
period 10/failure 30. Preserve them, scale, identity, networking and ingress.

API has `AI__Enabled=false`, no endpoint/model variables and currently no CORS
environment entry. The reviewed CORS addition is `Cors__AllowedOrigins__0`:
`https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io`
(no trailing slash). Existing JWT/admin environment entries use `secretRef`.
The API template now also sets `Forecasting__BackgroundWorkerEnabled=false`.
This prevents that worker from being registered, while leaving forecast services
available to explicit requests. The default remains enabled outside this explicit
portfolio setting.
The portfolio API also sets `DataRetention__BackgroundWorkerEnabled=false`, which
prevents startup and scheduled archive/delete cleanup from being registered, and
`AzureMaps__Enabled=false`, which prevents queued external-context jobs from making
Azure Maps requests. With `AI__Enabled=false`, incident AI, visual-analysis and
knowledge-document queue workers are not registered. Their request-facing services
remain available as disabled implementations; this prevents failed queue jobs from
writing failure-analysis rows in the portfolio environment.
Web retains its API base URL, Syncfusion secretRef, Blob Data Protection URI and
`DataProtection__UsePortfolioDemoUnwrappedKeys=true`. The reduced-security unwrapped
Blob key mode is restricted to synthetic PortfolioDemo data. Nothing changes these settings.

### Retained rollback artifacts

| Component | Existing immutable registry digest |
|---|---|
| API | `acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api@sha256:c09b299562f3eaae4786b2361f73e9a214c157f3d6d863b6ca1858e070a8cb4a` |
| Web | `acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b` |

Both old manifests were verified in ACR. Do not delete these images or revisions.

## Local artifacts and validation

The 29 October tags (`portfolio-stab-20261029-113132-65d9714`) and local digests
`sha256:d585d85681cdfbf2faff58d74256896e0758fc90d812e5e04e26a45c5adb3ac1` (API)
and `sha256:d9a8a39ec2c873777e9b8debd871cb06437964be4cd5d6dcd1cfab5a14e93387` (Web)
are historical and superseded. The 30 October dirty-tree build identifiers recorded
in the checkpoint note are also ineligible. The current Docker image store did not
contain those 30 October tags. No candidate is approved for push.

After a separately authorized clean source checkpoint, build from a Git archive of
that exact commit. This avoids including untracked or local environment files. Use a
new tag derived from that commit, retain BuildKit metadata and image IDs under ignored
`TestResults`, and record local manifest digests. The following is a template and was
not run because no clean commit is authorized in this task:

```powershell
$sourceCommit = '<40-character-reviewed-commit-sha>'
if ($sourceCommit -notmatch '^[0-9a-f]{40}$') { throw 'Use the reviewed clean commit SHA.' }
$release = 'portfolio-stab-20261030-' + $sourceCommit.Substring(0, 12)
$out = "TestResults/stabilization-$release"
$source = "$out/source"
New-Item -ItemType Directory -Force -Path $out,$source | Out-Null
rtk proxy git archive --format=tar --output="$out/source.tar" $sourceCommit
rtk proxy tar -xf "$out/source.tar" -C $source
rtk proxy docker buildx build --load --platform linux/amd64 --label "org.opencontainers.image.revision=$sourceCommit" --label org.opencontainers.image.source=https://github.com/AmarjeetJha17/AI-Emergency-Response --metadata-file "$out/api-build.json" --iidfile "$out/api-image.id" --tag "acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api:$release" --file "$source/docker/Dockerfile.api" $source
rtk proxy docker buildx build --load --platform linux/amd64 --label "org.opencontainers.image.revision=$sourceCommit" --label org.opencontainers.image.source=https://github.com/AmarjeetJha17/AI-Emergency-Response --metadata-file "$out/web-build.json" --iidfile "$out/web-image.id" --tag "acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web:$release" --file "$source/docker/Dockerfile.web" $source
```

Only after a separate ACR-write approval, push these newly built tags, query each
remote digest, compare it with build metadata and confirm that the tag did not
previously exist. A local build or digest does not prove remote availability. Do not
push the old or dirty-tree candidates.

## Property-level ARM What-If: gate CLOSED

The new helper is preview-only. It uses GET and the documented ARM What-If POST,
Incremental mode, FullResourcePayloads, validated asynchronous polling and a
completed inspectable changes requirement. It prints only IDs, paths and categories.
No before/after payloads, token or secret values are printed or written to disk.

```powershell
$apiImage = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api@sha256:<verified-api-digest>'
$webImage = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:<verified-web-digest>'
rtk proxy pwsh -NoProfile -File scripts/deployment/preview-portfolio-stabilization.ps1 -Component api -Image $apiImage -RevisionSuffix '<unique-api-suffix>'
rtk proxy pwsh -NoProfile -File scripts/deployment/preview-portfolio-stabilization.ps1 -Component web -Image $webImage -RevisionSuffix '<unique-web-suffix>'
```

These commands can inspect planned local digests before push; they do NOT verify
that ACA can pull them. Re-run with verified remote digests after any separately approved push.

The API completed preview returned **one Modify, 12 Ignore**, no resource creates,
deletes or replacements. Property paths/categories:

```text
properties.configuration.secrets [Delete]                 BLOCKER
properties.runningStatus [Delete]                        response-only
properties.template.containers [Array]
properties.template.containers[0] [Modify]
properties.template.containers[0].env [Array]
properties.template.containers[0].env[16] [Create]         Cors__AllowedOrigins__0
properties.template.containers[0].env[17] [Create]         Forecasting__BackgroundWorkerEnabled
properties.template.containers[0].image [Modify]
properties.template.revisionSuffix [Modify]
```

That recorded preview predates the forecast, retention and Azure Maps opt-out settings
added in the current source tree. It is historical evidence only; rerun the read-only
preview with the current image/template before rollout. The updated API preview gate
recognizes deltas only at the computed indexes of the four approved API environment
settings (CORS origin and the three explicit worker/external-call controls), while
rejecting deletions and changes to every other environment entry.

Additional `NoEffect` metadata: API identity clientId/principalId, ingress.fqdn,
container resources.ephemeralStorage. They are not permission or network updates;
the helper conservatively reports/rejects paths outside its narrow gate as well.
All other env entries, probes, ingress, scale and registry settings are copied from
the live revision/configuration in memory; no registry server change is needed now.

The Web completed preview also returned **one Modify, 12 Ignore**, with no resource
creates/deletes/replacements. Its complete distinct property-path summary is:

```text
properties.configuration.secrets [Delete]                 BLOCKER
properties.runningStatus [Delete]                        response-only
properties.template.containers [Array]
properties.template.containers[0] [Modify]
properties.template.containers[0].image [Modify]
properties.template.revisionSuffix [Modify]
identity.userAssignedIdentities['<existing Web identity resource ID>'].clientId [NoEffect]
identity.userAssignedIdentities['<existing Web identity resource ID>'].principalId [NoEffect]
properties.configuration.ingress.fqdn [NoEffect]
properties.template.containers[0].resources.ephemeralStorage [NoEffect]
```

API has the same distinct NoEffect paths using its existing API identity. Azure
repeated the two identity NoEffect entries in both responses. In each preview,
the twelve ignored resources are the other Container App, environment, Central US
registry, Key Vault, two managed identities, VNet, Log Analytics workspace, SQL
server, EmergencyResponse database, master database and storage account. The
cross-group East US 2 ACR is referenced but not deployed by either projection.
Both helper executions exited nonzero deliberately; neither passed the release gate.

The projection cannot yet meet the required clean gate: existing native secrets
returned by GET contain names but no values. Declaring those names without values
fails with allowlisted codes `InvalidTemplateDeployment` / `ContainerAppSecretInvalid`.
Omitting the app-scoped secret collection permits a completed preview but predicts
its deletion. **That deletion is not approved and is not treated as harmless.**
The helper is never an execution template. Do not fetch secret values, insert
disposable secret placeholders, or submit the full projection to bypass this blocker.

`runningStatus` is excluded from deployable properties in the
[2024-03-01 Create/Update contract](https://learn.microsoft.com/en-us/rest/api/resource-manager/containerapps/container-apps/create-or-update?view=rest-resource-manager-containerapps-2024-03-01).
Only that exact Delete is response-only; it does not justify ignoring secret deletion.

## Secret-preserving targeted update review — NOT authorized or ready to execute

The installed Azure CLI implementation was inspected at version 2.91.0. Its
`containerapp update` image and `--set-env-vars` path sends a PATCH and does not
construct `properties.configuration.secrets` unless registry credentials are being
changed. The command must omit `--yaml`, `--replace-env-vars`, `--secrets`, registry,
identity and ingress options. This is a source-level CLI behavior review, not an ARM
property-level What-If guarantee. The read-only helper
`scripts/deployment/compare-portfolio-stabilization-update.ps1` records a sanitized
pre-state in memory, then validates the post-state after the separately authorized
operator update. It fails closed if secret names/references, identity, registry,
ingress/traffic, scale, probes, networking, non-approved environment entries or
image differ. It does not submit the update. Run it in one terminal and perform the
approved CLI command in another; type `COMPARE` only after completion.

The commands below are templates only. Replace each digest placeholder with the
verified ACR digest built from the clean, reviewed source commit. Do not use tags or
digests listed in the historical candidate section. Verify each remote digest and
run the before/after comparator immediately around its component update. In Single
revision mode, do not update Web until API health and smoke gates pass.

```powershell
$sub = '0a05d5ff-7fef-4886-8e72-22a6cafd5567'
$rg = 'rg-er-portfolio-demo-centralus'
$apiImage = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api@sha256:<verified-api-digest>'
$webImage = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:<verified-web-digest>'
# Choose a new, lowercase ACA revision suffix for each app update. The comparator
# validates the format and confirms the full suffix is not already in use.
$apiRevisionSuffix = 'api-261030-r1'
$webRevisionSuffix = 'web-261030-r1'

# Terminal A: capture the API baseline; after the separately approved update, type COMPARE.
rtk proxy pwsh -NoProfile -File scripts/deployment/compare-portfolio-stabilization-update.ps1 -Component api -Image $apiImage -RevisionSuffix $apiRevisionSuffix

# Terminal B, only after source/image/digest and baseline reviews are complete:
rtk proxy az containerapp update --subscription $sub -g $rg -n ca-er-api-portfolio --container-name api --image $apiImage --revision-suffix $apiRevisionSuffix --set-env-vars AI__Enabled=false Forecasting__BackgroundWorkerEnabled=false DataRetention__BackgroundWorkerEnabled=false AzureMaps__Enabled=false Cors__AllowedOrigins__0=https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io --output none --only-show-errors

# After API readiness, auth/CORS and dashboard checks pass, repeat the comparator for Web,
# then run only the Web image update (no environment or secret arguments).
rtk proxy pwsh -NoProfile -File scripts/deployment/compare-portfolio-stabilization-update.ps1 -Component web -Image $webImage -RevisionSuffix $webRevisionSuffix
rtk proxy az containerapp update --subscription $sub -g $rg -n ca-er-web-portfolio --container-name web --image $webImage --revision-suffix $webRevisionSuffix --output none --only-show-errors
```

`--set-env-vars` adds or updates only named environment entries; it does not remove
other entries. Nevertheless, compare the post-state and stop if the actual CLI/API
behavior differs from this reviewed version. Azure CLI updates do not provide the
same property-level What-If guarantee as ARM What-If. Never interpret the preview
helper's resource projection as an executable deployment template.

## Additional rollout constraints

The targeted-update review above supersedes the former full-resource projection
rollout draft. Do not run native-secret deployment helpers, full-stack Bicep,
secret-set/list operations, `--replace-env-vars`, registry/identity/ingress changes,
or a full GET-payload PUT. If any update outcome is ambiguous, first inspect revision
state read-only; do not automatically retry. The historical ARM What-If deletion of
`properties.configuration.secrets` remains a blocker to using the Bicep projection,
but is not a reason to submit that projection or relax its gate.

### Single-mode health gates and rollback

In [Single mode](https://learn.microsoft.com/en-us/azure/container-apps/revisions),
Azure retains old traffic until the replacement is ready, then automatically switches
100% to it. There is no manual canary gate between revision readiness and traffic
switching in this design. Do not switch revision modes or apply Multiple-mode traffic commands.

For each app, verify ProvisioningState Succeeded, exact image digest, latest=ready,
Healthy/Provisioned revision, 100% latest traffic, unchanged probes/identity/secretRef
bindings, `/health/live` and `/health/ready` HTTP 200. SQL readiness must succeed;
serverless cold-start delays are not a reason to weaken probes or permissions.
If API fails any gate, do not update Web. If Web fails, roll Web back first; roll API
back too if its gates fail or the combined release must be reverted.

**Future rollback only after explicit approval; not executed.** Use targeted image
updates to the previously verified digest. This creates a new revision while
preserving the current configuration and native secrets. Do not use `revision copy`:
it restores the old revision's template and would remove the new API environment
settings, including CORS and background-worker gates.

```powershell
rtk proxy az containerapp update --subscription $sub -g $rg -n ca-er-web-portfolio --container-name web --image acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b --output none --only-show-errors
rtk proxy az containerapp update --subscription $sub -g $rg -n ca-er-api-portfolio --container-name api --image acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api@sha256:c09b299562f3eaae4786b2361f73e9a214c157f3d6d863b6ca1858e070a8cb4a --set-env-vars AI__Enabled=false Forecasting__BackgroundWorkerEnabled=false DataRetention__BackgroundWorkerEnabled=false AzureMaps__Enabled=false Cors__AllowedOrigins__0=https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io --output none --only-show-errors
```

Re-check mode, old revision availability and digest before rollback; if drifted, stop.
Repeat readiness, traffic and authentication checks after each rollback update. The
API targeted rollback command deliberately retains all portfolio flags and CORS; it
does not restore the previous CORS failure. A rollback is not a database restore and
cannot undo startup writes, queued-worker writes, rotated secrets or Data Protection
key changes.

The retained API rollback image predates the new forecast and retention registration
guards. Setting their environment variables during rollback cannot guarantee those
old binaries honor the values; an API rollback can therefore re-enable the old
scheduled worker behavior. Treat that rollback as a database-write risk and inspect
the old binary's source/configuration behavior before authorizing it. The API image
rollback also restores an older release, not a pure code rollback with the new guards.

## Post-deployment read-only smoke plan

Do not execute this against candidates before deployment. Existing authoritative
URLs are Web `https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io`
and API `https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io`.

```powershell
rtk proxy az containerapp revision list --subscription $sub -g $rg -n ca-er-api-portfolio --query "[].{name:name,active:properties.active,traffic:properties.trafficWeight,health:properties.healthState,state:properties.provisioningState,images:properties.template.containers[].image}" -o json
rtk proxy az containerapp revision list --subscription $sub -g $rg -n ca-er-web-portfolio --query "[].{name:name,active:properties.active,traffic:properties.trafficWeight,health:properties.healthState,state:properties.provisioningState,images:properties.template.containers[].image}" -o json
rtk proxy curl.exe -sS -o NUL -w '%{http_code}' https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/health/ready
rtk proxy curl.exe -sS -o NUL -w '%{http_code}' https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/health/ready
```

1. Follow [the CORS runbook](signalr-cors-verification.md): trusted OPTIONS 204 with
   exact allow-origin, credentials true and required headers/method; untrusted origin
   has no CORS grant. Unauthenticated and invalid-bearer negotiate remain 401.
2. In the existing authorized browser session, negotiate must return 200 and establish
   a transport. Do not export HAR, print tokens or invoke hub broadcasts. Block the
   dashboard's forecast requests in the temporary test browser before loading it.
3. Authorized `GET /api/dashboard/summary` and `GET /api/map/incidents` are read-only.
   With unchanged data and all active reports located, expect 16 active on both,
   excluding Closed and Rejected. Compare the same unfiltered snapshot; unlocated
   active incidents legitimately make the map lower. Do not edit records to force equality.
4. Authorized incident list/detail/history/location reads must still work for all 24
   synthetic incidents. Detail must report `VisualAnalysisEnabled=false`; upload copy
   must say analysis disabled/evidence-only, never promise automatic analysis.
   Do not upload files, reprocess, dispatch, mark notifications read or run AI actions.
5. Read only fixed diagnostic-code aggregates from existing logs. No full raw log dump.
   Do not invoke `/Dashboard/IncidentVolume`, `/api/predictive/incident-volume`,
   `/api/predictive/regenerate`, or resource-demand forecasting.

## Remaining blockers and final checkpoint

- **Clean secret-preserving ARM preview is not established.** Both completed projections
  predict secret deletion and are rejected, as recorded above.
  A resource PUT What-If is not proof of the narrower future PATCH. Do not relax the gate.
- **Candidate manifests are local only.** Push is prohibited this task; obtain separate
  registry-write authorization, verify remote digests, then redo previews.
- **Strict zero-startup-write guarantee cannot be made.** Staging skips EF migrations,
  but `Program.cs` still calls `DatabaseSeeder`, `RoleSeeder` and `AdminSeeder`. They
  read first and insert only missing reference rows, roles, or the configured admin;
  they do not update or delete existing rows. The earlier hosted snapshot showed the
  expected reference data and admin present, but it is not a fresh check. Recheck these
  rows before rollout if startup writes are unacceptable.
- Retention cleanup is excluded by `DataRetention__BackgroundWorkerEnabled=false`;
  forecast generation is excluded by `Forecasting__BackgroundWorkerEnabled=false`.
  Forecast endpoints/services remain callable and may persist results when explicitly
  invoked.
- The outbox publisher and incident-situation evaluator remain registered. With queued
  work, they can update outbox attempt/publication state and create or update operational
  situations, notifications, and audit records. They support event-driven operational
  workflows and are intentionally preserved. Situation analysis resolves to a disabled
  AI implementation with `AI__Enabled=false`, so it does not call a model provider.
- The predictive-analysis worker remains registered and can persist prediction snapshots
  and results when a job is queued. Its current implementation uses local ML.NET inference,
  not paid Azure AI. It is distinct from forecast generation and remains available to
  event-driven/manual predictive workflows.
- The Service Bus consumer is registered only for Production or explicit AzureServiceBus
  mode. The portfolio template uses Staging and does not select that provider, so this
  deployment uses the in-memory bus and does not start the durable consumer. The outbox
  publisher remains active and can persist attempts for pending outbox entries.
- No API startup or worker was run locally for this review, and no restart is authorized;
  current database state may have changed and must be verified before rollout.
- Forecasting remains unavailable/unverified due to the unchanged native runtime.
  No model endpoint was invoked. Existing package vulnerability warnings remain.
- Local tests and readiness do not prove end-to-end CORS, hub auth, map or UI success
  for images that have not been deployed. Keep the final execution checkpoint closed.

No Azure SQL, access permissions, identities, networking, private storage, Key Vault,
native secrets, AI settings or live applications were changed during this preparation.

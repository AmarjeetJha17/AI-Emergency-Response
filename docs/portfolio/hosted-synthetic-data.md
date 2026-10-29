# Hosted synthetic incident dataset

This opt-in workflow prepares `PORTFOLIO24-v1`. It is separate from the older
six-incident DEMO18 exercise and never runs during API startup. Preparation does
not authorize an Azure SQL write. Obtain separate explicit approval before using
write mode, even if database credentials already permit inserts.

## Composition

The dataset contains 24 authored scenarios, four each for flood, fire, road
accident, landslide, building collapse and medical emergency. Approximate city
coordinates cover Pune, Mumbai, Bengaluru, Rishikesh, Delhi and Chennai; all site
addresses are invented. Incident numbers `SYN24-01` through `SYN24-24`, titles,
descriptions, locations and history remarks identify synthetic data.

Each severity and priority has six incidents. Submitted, UnderReview, InProgress,
Resolved, Closed and Rejected each have four. Assigned is deliberately absent
because no assignment or dispatch is fabricated. Status histories start at
Submitted and end at the authored current status. Dates span the preceding
14 days relative to the first import, avoiding future timestamps.

Expected first-import inserts:

| Table | Rows |
| --- | ---: |
| Incidents | 24 |
| IncidentLocations | 24 |
| IncidentStatusHistory | 72 |
| AuditLogs (dataset ownership marker) | 1 |
| All other tables | 0 |

The dashboard should gain six incidents per severity, 16 active incidents and 24
unassigned incidents when starting from an empty operational database. Its review,
resource, escalation and situation counts stay unchanged. This dataset demonstrates
incident lists, maps, history and dashboard counts; it does not populate every
dashboard panel. Severity/priority labels are authored exercise values. Scoring
pipelines do not run. Casualties and impact counts remain unknown; no AI analyses,
embeddings, confidence estimates or confirmed duplicate conclusions are inserted.

## Read-only preview

For the dedicated Entra user proposal, inspection SQL and separately approved
provisioning workflow, see [Seeding principal preparation](seeding-principal.md).

From the repository root in an authorized terminal, configure a password-free
Entra connection. This contains no credentials:

```powershell
$env:PORTFOLIO_SEED_CONNECTION = 'Server=sql-er-demo-qqgqkekj.database.windows.net;Database=EmergencyResponse;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False'
# Set this non-secret variable to the SQL SID of the operator-approved direct
# Entra database user, confirmed through an authorized administrative channel.
# Do not derive approval merely from whichever account the CLI happens to use.
dotnet run --project .\tools\EmergencyResponse.DemoData\EmergencyResponse.DemoData.csproj --configuration Release -- hosted-preview --expected-principal-sid $approvedPrincipalSid
```

Use an existing approved network path and the operator-approved Entra principal.
Preview checks SELECT and the required INSERT permissions without exercising
INSERT. Preview does not create users, migrate schema, seed reference
tables, change firewall rules, or open a write transaction. Its JSON contains only
dataset identity, target, verified SQL principal SID, current table counts, planned
counts, ownership state and an approval fingerprint.
It does not output users, incident bodies, connection strings or credentials.
The command includes the SqlClient Azure authentication extension required by
SqlClient 7.1.1. No application host or workers are started.

Preview requires the six active incident type codes to exist. Departments, users,
roles and reference types are read/preserved. Incidents have no reporter identity;
status histories have no impersonated user. Access via a Citizen account will
therefore not imply ownership; use an existing authorized operational account to
view the exercise.

The Azure target is allowlisted to the exact server/database above and requires
encrypted Entra authentication. The connection allowlist retains only
`EmergencyResponse_PortfolioDemo` for local targets; public write mode also
requires the SQL external-principal verification (Windows/SQL logins cannot bypass
it). The existing `EmergencyResponseDb` is rejected. Isolated unit rehearsals use
isolated SQLite fixtures. The attempted LocalDB read of `EmergencyResponseDb`
was unavailable in the preparation environment; its 12 reported examples were
not copied or independently verified.

## Separate write stage (not authorized or executed during preparation)

After separate approval, `hosted-seed` is the dedicated write command. It first
runs the same preview and requires explicit `--expected-principal-sid` and
`--expected-fingerprint` inputs. It rejects duplicate/unknown arguments, missing
values, whitespace, wrong versions and malformed fingerprints. The fingerprint
must be the exact `HPFP1:` plus 64 uppercase hex digits from the reviewed preview.
It is not a substitute for authorization. The command still requires the interactive exact phrase
`SEED SYNTHETIC PORTFOLIO24`. Redirected input is rejected. No command argument or
environment variable bypasses this confirmation. It requires INSERT access to
only the four tables above in addition to SELECT. Do not grant runtime DDL rights
or broaden the deployed API identity for this workflow.

The write uses one serializable EF transaction, stable SHA-256-derived GUIDs and
the ownership marker. Fresh reads inside that transaction recompute the fingerprint
and verify the SQL principal/permissions again before inserting anything. The hash
covers dataset version, target, verified principal SID, sorted existing/planned
counts, full scalar incident-type/department/reference-requirement data, role
identity/version fields and all ownership-marker fields. It deliberately excludes
wall-clock time and connection credentials. A fresh approval is required after
the first seed because the counts and ownership state change. A second execution
with its fresh reviewed fingerprint performs no inserts and preserves edited
rows. Colliding incident numbers/IDs, missing references or incomplete dataset
relationships stop the operation, rather than adopting, repairing or overwriting
rows. Errors are sanitized and no automatic retry occurs. An interrupted client
can leave the commit outcome uncertain: run preview again before deciding what
to do. No cleanup/delete command is provided for this dataset.

The SID input is a SQL SID expressed as `0x` followed by 32 or 36 hexadecimal
digits, not a token or connection credential. Use the exact SID confirmed by an
authorized administrator; do not paste credentials into command arguments.
The tool reads `sys.database_principals` for the effective session principal,
requires a direct external user (`type=E`, external authentication), and requires
the original login, current login and database-user name to match. It rejects
dbo/guest, group-only sessions and ambiguous/impersonated contexts. Database-user
aliases whose names differ from the login are deliberately unsupported. A
previous successful preview under an administrator mapped to dbo will now fail
closed; this tool never creates a replacement user or grants access.

Minimum effective permissions: CONNECT to the target database; SELECT on
`dbo.Incidents`, `IncidentLocations`, `IncidentStatusHistory`, `AuditLogs`,
`IncidentTypes`, `IncidentTypeResourceRequirements`, `Departments`, `AspNetUsers`,
`AspNetRoles`, `Resources`, `IncidentAssignments`, `IncidentEscalations`, and
`OperationalSituations`; INSERT on `Incidents`, `IncidentLocations`,
`IncidentStatusHistory`, and `AuditLogs`. Own-principal catalog visibility and
`HAS_PERMS_BY_NAME` must be available. Permission checks treat NULL/unknown as
failure. UPDATE, DELETE, ALTER, CONTROL and db_owner are not required. Grants,
RBAC changes and identity creation are administrator tasks outside this tool.

Regression tests use a real isolated SQLite transaction, including a failure
after all 121 inserts were saved but before commit, followed by database reads
confirming all 121 were rolled back. Identity-policy tests use synthetic session
metadata; SQL session queries and Azure permission checks are not verified live
during preparation. SQL Server locking/deadlock behavior still needs a separately
approved isolated integration environment. Never test rollback against the hosted
portfolio database.

The importer directly persists entities without publishing events, outbox messages,
notifications, dispatches, assignments or AI jobs. Existing deployed services and
manual actions remain outside the importer; keep AI disabled and do not perform
real operational actions on synthetic records.

## Local verification

```powershell
dotnet test .\tests\EmergencyResponse.UnitTests\EmergencyResponse.UnitTests.csproj --configuration Release --filter FullyQualifiedName~HostedPortfolioSeederTests
dotnet build .\tools\EmergencyResponse.DemoData\EmergencyResponse.DemoData.csproj --configuration Release
git diff --check
```

SQLite tests verify relationships and atomic/idempotent behavior, but do not prove
Azure network access, SQL Server concurrency behavior or Entra permissions. A
successful Azure preview is still required before approving a write.

## Read-only authentication probe

With the existing `PORTFOLIO_SEED_CONNECTION` configured, run:

```powershell
dotnet run --project .\tools\EmergencyResponse.DemoData\EmergencyResponse.DemoData.csproj --configuration Release -- hosted-auth-check
```

This command validates the allowlisted target and encryption before acquiring a
token through `DefaultAzureCredential` for `https://database.windows.net/.default`.
It prints `TOKEN_PASS` or `TOKEN_FAIL` (with sanitized exception type on failure).
On success it opens a fresh SQL connection with that exact token, reporting
`SQL_OPEN_PASS` or `SQL_OPEN_FAIL`. Explicit-token connection settings remove
Authentication, User ID, Password and Integrated Security, disable pooling, and
preserve encryption/certificate validation. The original environment value is
never modified. Explicit-token failures may include at most eight numeric SQL
error triples, hexadecimal HRESULT and immediate inner exception type.

Only after `SQL_OPEN_PASS`, the probe opens a separate connection using the
original connection string and prints `SQL_PROVIDER_OPEN_PASS` or
`SQL_PROVIDER_OPEN_FAIL`. This can distinguish a provider-path difference from
failure of the explicit-token path; it does not by itself prove the underlying
cause. There are no application queries, EF host, seed calls, migrations or
writes. Tokens are never printed or persisted by the tool; local references are
cleared after the explicit-token step. Immutable token strings cannot be securely
zeroed. A two-minute cancellation budget bounds the command's credential/connection
operations. The probe does not authorize seeding or alter the write gate.

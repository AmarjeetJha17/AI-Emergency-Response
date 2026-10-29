# Least-privilege portfolio seeding principal: preparation only

No principal, permission or Entra identity has been created by this preparation.
Neither provisioning nor portfolio data seeding is authorized by this document.

## Proposed identity

Use a dedicated, tenant-member Entra human user approved for synthetic imports.
Its actual UPN/object ID must be supplied by the operator; none is invented here.
It must not be the SQL Entra administrator, a member of that administrator group,
the API/Web managed identity, or an account with inherited elevated SQL access.
Map the exact unique human-user UPN with `FROM EXTERNAL PROVIDER`, then verify
the created SID equals the approved user object ID before grants or commit.
Do not use the OBJECT_ID alias extension for this human-user mapping: Azure SQL
rejected it with error 37535. Do not use a display-name alias or group mapping: the verifier requires
type `E`, external authentication, approved SID equality and equality of original
login, current login and database-user name. A purpose-specific service principal
with certificate/federated authentication is an alternative for future automation,
but its login/name/SID behavior must be separately verified; the prepared SQL
provisioning script intentionally supports the human-user proposal only.

The existing `bootstrap-portfolio-demo-database.sql` grants the API runtime user
db_datareader/db_datawriter. Do not reuse or execute it for seeding. Its broader
runtime role grants are outside the dedicated seeding principal's minimum grants.
Repository documentation reports that mapping exists; no live principal inventory
was fetched during this task.

## Read-only inspection

Connect through the existing approved network path to
`sql-er-demo-qqgqkekj.database.windows.net`, database `EmergencyResponse`, in SSMS
using Microsoft Entra MFA. Open and execute only:

`scripts/deployment/inspect-portfolio-seed-principals.sql`

An authorized administrator first inspects existing users, SID lengths, explicit
grants and database-role memberships. A limited account may not see every catalog
row. Direct grants/roles do not prove another account's effective permissions.
Reconnect as the proposed user and rerun the same script to inspect the actual
session and `HAS_PERMS_BY_NAME` results. Do not use `EXECUTE AS` to stand in for a
real login; that cannot demonstrate compliance with the seeder's original-context
gate. Required results must be 1, not NULL. Compare the visible table inventory to
the administrator's inventory, and review inherited Entra-group/public permissions.
If there are additional INSERT, UPDATE, DELETE, ALTER, CONTROL or DDL permissions,
stop for administrative review; do not remove or add permissions automatically.

These smaller read-only queries identify the session and its SQL SID:

```sql
SELECT ORIGINAL_LOGIN() AS OriginalLogin, SUSER_SNAME() AS CurrentLogin,
       USER_NAME() AS DatabaseUser, DB_NAME() AS DatabaseName;
SELECT name, type, authentication_type_desc, DATALENGTH(sid) AS SidBytes,
       CONVERT(varchar(170), sid, 1) AS SqlSid
FROM sys.database_principals WHERE principal_id = DATABASE_PRINCIPAL_ID();
```

The SID must belong to the approved user, not dbo. Have an administrator correlate
it with the approved Entra object ID. For a human contained user, SQL stores the
16-byte representation of that object ID: 32 hex digits following `0x`. The current
validator accepts that representation already; no widening is needed for a 32-byte
(64-hex-digit) dbo SID. Login-based external users can have an 18-byte SID ending
AADE, which explains the existing 36-digit alternative. This proposal does not
create a server login. Copy SQL's actual hex SID rather than removing hyphens from
an object-ID GUID; byte ordering matters.

## Required permissions

CONNECT to `EmergencyResponse`; SELECT on these 13 dbo tables:

- Incidents, IncidentLocations, IncidentStatusHistory, AuditLogs
- IncidentTypes, IncidentTypeResourceRequirements, Departments
- AspNetUsers, AspNetRoles, Resources, IncidentAssignments
- IncidentEscalations, OperationalSituations

INSERT on **only** dbo.Incidents, dbo.IncidentLocations,
dbo.IncidentStatusHistory and dbo.AuditLogs. The ownership marker is the one
AuditLogs insert, not a separate schema object. Own-principal catalog visibility
and `HAS_PERMS_BY_NAME` must work. No db_owner, db_datareader, db_datawriter,
db_ddladmin, schema-wide grants, UPDATE, DELETE, DDL, ownership or grant option is
needed for this principal. No Azure resource role is required merely to connect
to SQL as a contained user. Existing network restrictions still apply.

## Provisioning: separate future approval required

Prepared file: `scripts/deployment/provision-portfolio-seed-principal.sql`.
It is disabled by NULL values for the approved UPN, user object ID and approval
phrase. Only after separate provisioning approval, an authorized administrator
fills those non-secret fields in an approved working copy and reviews the exact
target and grant list. Do not include passwords/tokens. The script:

- Checks the exact logical server/database and distinct UPN.
- Refuses any existing name/SID rather than adding privileges to an existing user.
- Creates one contained external user by its unique UPN, then verifies its exact object-ID/SID binding before grants or commit.
- Verifies type E, external authentication and the expected 16-byte SID.
- Grants CONNECT and the table permissions above within one transaction.
- Rolls back on failure and emits a fixed diagnostic without directory messages.

It creates no Entra identity and modifies no Azure RBAC/network settings. Directory
validation for `FROM EXTERNAL PROVIDER` may require existing Microsoft Graph access
for the provisioning context. If unavailable, stop; do not bypass directory
validation or grant Directory Readers automatically. Existing public/group/server
permissions can still affect effective rights: reconnect as the new user and
inspect before approving any seed. This script's completion is not seed approval.

## Password-free console authentication

Use a dedicated local CLI profile so an administrator's cached identity cannot be
selected accidentally. The following are manual local authentication steps, not
identity provisioning or permission grants. Use operator-approved tenant/UPN/SID
values; none is a token or secret:

```powershell
$env:AZURE_CONFIG_DIR = Join-Path $env:LOCALAPPDATA 'PortfolioSeederAzureCli'
az login --allow-no-subscriptions --tenant $approvedTenantId
az ad signed-in-user show --query '{id:id,userPrincipalName:userPrincipalName}' --output json
$env:AZURE_TOKEN_CREDENTIALS = 'AzureCliCredential'
$env:PORTFOLIO_SEED_CONNECTION = 'Server=sql-er-demo-qqgqkekj.database.windows.net;Database=EmergencyResponse;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False'
dotnet run --project .\tools\EmergencyResponse.DemoData\EmergencyResponse.DemoData.csproj --configuration Release -- hosted-preview --expected-principal-sid $approvedPrincipalSid
```

Sign in interactively as the dedicated member user, completing MFA, and compare
the returned user ID/UPN to the approved directory identity. Do not print access
tokens or pass a password. Selecting AzureCliCredential avoids other cached
developer identities in the DefaultAzureCredential chain, supported by the
repository's Azure.Identity version. The SQL SID/original-context checks remain
mandatory: CLI identity output is supporting evidence, not SQL identity proof.
Do not add User ID to select a human identity in Active Directory Default; its
meaning can differ for managed identities.

The environment selection does not bypass any SQL permission, target, fingerprint
or interactive gate. After this read-only preview succeeds, its verified principal
SID and fresh fingerprint still need separate seed approval. This task provides
no seed write command. Restore/close the dedicated shell after use; do not share
its local credential cache or enable CLI/SQL debug/token logging.

## Evidence and limitations

- [Microsoft: contained vs login-based external SID representations](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-aad-overview?view=azuresql)
- [Microsoft: CREATE USER and OBJECT_ID binding](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-user-transact-sql?view=sql-server-ver17)
- [Microsoft: effective permissions for the current session](https://learn.microsoft.com/en-us/sql/t-sql/functions/has-perms-by-name-transact-sql?view=sql-server-ver17)
- [Microsoft: selecting AzureCliCredential in DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains?tabs=dac)

Local regression tests confirm the validator accepts a synthetic 16-byte user SID
and continues rejecting dbo-sized SIDs, groups and impersonation. The SQL scripts
are prepared/reviewed text, not executed or validated against Azure. The actual
approved identity, tenant membership, catalog inventory, permission inheritance,
directory provisioning access and original-login/UPN behavior remain to be
verified by authorized read-only inspection. No safeguards were weakened and no
transaction, dataset, fingerprint or confirmation behavior changed.

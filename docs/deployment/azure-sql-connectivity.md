# Azure SQL Connectivity

## Staging target

The Bicep foundation defines an Azure SQL logical server and `EmergencyResponse` database with Microsoft Entra-only authentication and `publicNetworkAccess` disabled. It creates the `privatelink.database.windows.net` private DNS zone, links it to the staging VNet, and associates the SQL private endpoint with a private DNS zone group.

The application must connect with the ordinary server FQDN; VNet DNS resolves that name to the private endpoint. Never configure a private endpoint IP as the SQL server name.

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;
Database=EmergencyResponse;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

Expose the value under the actual application key `ConnectionStrings__DefaultConnection`. Validate the installed Microsoft.Data.SqlClient/Azure Identity versions and the Container Apps managed identity connection before pilot traffic.

## Database users and migrations

After the API and Worker identities are deployed, connect through an approved private network path as the Entra SQL administrator or deployment identity and create contained users for those identities. Grant the runtime users only required data permissions; do not grant `db_owner`. Run reviewed EF migration bundles or SQL scripts under a separate deployment identity with schema-change permissions.

The preview does not create contained users, apply migrations, or prove runtime SQL access. The staging database uses synthetic data only and must remain isolated from local tests and pilot data.

## Verification after an authorized deployment

From a workload in the Container Apps VNet, verify:

1. `nslookup <sql-server-name>.database.windows.net` resolves to the private endpoint path.
2. TCP connectivity to port 1433 succeeds from the API/Worker network.
3. The endpoint connection state is approved and SQL public network access remains disabled.
4. The API identity can open its database connection and perform only its required operations.
5. `SELECT DB_NAME()` and `SELECT USER_NAME()` report the expected database and contained identity.

Do not use a local administrator connection as proof that the application identity has the needed authorization.

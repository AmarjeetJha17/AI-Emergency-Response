# Encryption Architecture

## In transit

Production traffic must use HTTPS/TLS. Restrict service-to-service and data-service network paths in the deployment environment.

## Database

Local development remains SQL Server Developer. Production is intended for Azure SQL Database with Microsoft Entra authentication, TLS, firewall/private-network restrictions, auditing, least-privilege runtime access, and tested backup/restore. Azure SQL TDE protects database files, transaction logs, and backups at rest by default. Always Encrypted is a selective option for columns whose threat model requires protection from database administrators; it is not enabled globally by this application.

## Object storage

Production media and knowledge documents belong in private Azure Storage containers with anonymous access disabled, HTTPS required, managed identity, minimum RBAC, and an approved soft-delete/versioning and recovery configuration. Azure Storage encrypts data at rest by default with Microsoft-managed keys. Customer-managed keys are an organizational choice when key control, auditing, rotation, or revocation requirements call for them. The application should serve files through authorized endpoints, not public blob URLs.

## Secrets and identity

Production secret configuration can load from Azure Key Vault using `Azure.Identity` and `DefaultAzureCredential`; Azure-hosted workloads should use managed identity, while local development can use developer credentials. Do not place long-lived Azure OpenAI, Maps, SQL, Storage, Search, JWT, or scanner secrets in source control or browser-visible configuration. Separate runtime and migration identities; the runtime identity should not be `db_owner`.

Key Vault loading is opt-in through `KeyVault:VaultUri`. Access requires the hosting identity to have only the necessary secret-read permissions and network access to the vault.

## Application Data Protection

The Web cookie host can persist ASP.NET Core Data Protection keys in a shared Azure Blob and protect them with a Key Vault key. Configure both `DataProtection:BlobUri` and `DataProtection:KeyIdentifier`; the key identifier should be versionless to support automatic key rotation. The application name is shared across instances of this Web application so cookies remain interoperable. Local development keeps the framework's local key store when these settings are absent.

## Backups and recovery

Configure Azure SQL automated backups and point-in-time restore, plus Blob versioning/soft delete as appropriate. Prove recovery by restoring into an isolated environment, checking database integrity and EF compatibility, running application smoke checks, and confirming media references resolve. Backup creation alone is not a verified recovery mechanism.

## Microsoft guidance

- [ASP.NET Core Data Protection configuration](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview)
- [Azure Blob Storage key store for ASP.NET Core Data Protection](https://learn.microsoft.com/dotnet/api/overview/azure/extensions.aspnetcore.dataprotection.blobs-readme)
- [Azure SQL Transparent Data Encryption](https://learn.microsoft.com/azure/azure-sql/database/transparent-data-encryption-tde-overview)
- [Azure Storage encryption at rest](https://learn.microsoft.com/azure/storage/common/storage-service-encryption)

-- Execute against the EmergencyResponse staging database as its approved
-- Microsoft Entra administrator. The application and migration identities
-- must already exist in the tenant. Do not run this against production.

IF DB_NAME() <> N'EmergencyResponse'
    THROW 51000, 'Connected to an unexpected database; refusing to bootstrap identities.', 1;

IF DATABASE_PRINCIPAL_ID(N'id-er-staging-api') IS NULL
    CREATE USER [id-er-staging-api] FROM EXTERNAL PROVIDER;

IF IS_ROLEMEMBER(N'db_datareader', N'id-er-staging-api') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [id-er-staging-api];

IF IS_ROLEMEMBER(N'db_datawriter', N'id-er-staging-api') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER [id-er-staging-api];

-- Created by infra/staging-migration-runner.bicep for a single controlled
-- deployment. Remove this database user after the migration exercise and
-- before deleting the temporary identity.
IF DATABASE_PRINCIPAL_ID(N'id-er-staging-migration') IS NULL
    CREATE USER [id-er-staging-migration] FROM EXTERNAL PROVIDER;

IF IS_ROLEMEMBER(N'db_datareader', N'id-er-staging-migration') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [id-er-staging-migration];

IF IS_ROLEMEMBER(N'db_datawriter', N'id-er-staging-migration') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER [id-er-staging-migration];

IF IS_ROLEMEMBER(N'db_ddladmin', N'id-er-staging-migration') <> 1
    ALTER ROLE [db_ddladmin] ADD MEMBER [id-er-staging-migration];

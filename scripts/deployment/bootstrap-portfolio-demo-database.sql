-- Run against the isolated portfolio-demo EmergencyResponse database as its
-- approved Microsoft Entra administrator. Never use operational data here.

IF DB_NAME() <> N'EmergencyResponse'
    THROW 51000, 'Connected to an unexpected database; refusing to bootstrap the portfolio API identity.', 1;

-- Pass the identity's current principal/object ID from Azure as a sqlcmd
-- variable. This avoids ambiguous tenant-wide display-name resolution.
DECLARE @ApiIdentityObjectId uniqueidentifier =
    TRY_CONVERT(uniqueidentifier, N'$(ApiIdentityObjectId)');

IF @ApiIdentityObjectId IS NULL
    THROW 51001, 'A valid ApiIdentityObjectId sqlcmd variable is required.', 1;

IF DATABASE_PRINCIPAL_ID(N'id-er-portfolio-api') IS NULL
BEGIN
    DECLARE @CreateApiUser nvarchar(max) =
        N'CREATE USER [id-er-portfolio-api] FROM EXTERNAL PROVIDER WITH OBJECT_ID = ''' +
        CONVERT(nvarchar(36), @ApiIdentityObjectId) + N'''';
    EXEC sys.sp_executesql @CreateApiUser;
END;

IF IS_ROLEMEMBER(N'db_datareader', N'id-er-portfolio-api') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [id-er-portfolio-api];

IF IS_ROLEMEMBER(N'db_datawriter', N'id-er-portfolio-api') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER [id-er-portfolio-api];

-- Apply reviewed EF migrations as the Entra administrator or a separate,
-- short-lived migration principal. The API runtime identity receives no DDL role.

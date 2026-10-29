-- PREPARATION ONLY: do not execute without separate explicit approval.
-- Run as an authorized Entra database administrator in the exact target.
-- Fill only approved NON-SECRET identity metadata and the approval phrase after
-- review. Never insert passwords, tokens or a connection string in this file.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ApprovedUpn sysname = NULL;
DECLARE @ApprovedUserObjectId uniqueidentifier = NULL;
DECLARE @Approval nvarchar(80) = NULL;

IF @Approval IS NULL OR @Approval <> N'PROVISION PORTFOLIO SEED PRINCIPAL'
    THROW 51110, 'Separate provisioning approval is required.', 1;
IF DB_NAME() <> N'EmergencyResponse' OR
   LOWER(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'))) NOT IN
       (N'sql-er-demo-qqgqkekj', N'sql-er-demo-qqgqkekj.database.windows.net')
    THROW 51111, 'Wrong server or database for provisioning.', 1;
IF @ApprovedUpn IS NULL OR @ApprovedUserObjectId IS NULL OR
   LEN(@ApprovedUpn) < 3 OR CHARINDEX(N'@', @ApprovedUpn) = 0 OR
   @ApprovedUpn <> LTRIM(RTRIM(@ApprovedUpn)) OR
   @ApprovedUpn = ORIGINAL_LOGIN() OR
   @ApprovedUpn IN (N'dbo', N'id-er-portfolio-api', N'id-er-portfolio-web')
    THROW 51112, 'Approved distinct Entra member-user UPN and object ID are required.', 1;

-- Fixed diagnostic stages; never assign identity or directory text here.
DECLARE @Stage varchar(4) = 'P001';
BEGIN TRY
    SET @Stage = 'P001'; -- Transaction start
    BEGIN TRANSACTION;
    -- Never add privileges to or adopt an existing principal. Review existing
    -- users with the read-only script; this script is for a NEW mapping only.
    SET @Stage = 'P002'; -- Existing-principal check
    IF EXISTS (SELECT 1 FROM sys.database_principals
               WHERE name = @ApprovedUpn OR sid = CONVERT(binary(16), @ApprovedUserObjectId))
        THROW 51113, 'Principal already exists; inspect and review it separately.', 1;

    -- Resolve the unique human-user UPN without the OBJECT_ID alias extension.
    -- P004 verifies the exact approved object-ID/SID before any grants/commit.
    -- Exact UPN (no alias) preserves the original-login/name equality check.
    SET @Stage = 'P003'; -- CREATE USER preparation and execution
    DECLARE @CreateUser nvarchar(max) = N'CREATE USER ' + QUOTENAME(@ApprovedUpn) +
        N' FROM EXTERNAL PROVIDER;';
    EXEC sys.sp_executesql @CreateUser;

    SET @Stage = 'P004'; -- Post-create type/SID verification
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals
                   WHERE name = @ApprovedUpn AND type = 'E'
                     AND authentication_type_desc = 'EXTERNAL'
                     AND sid = CONVERT(binary(16), @ApprovedUserObjectId)
                     AND DATALENGTH(sid) = 16)
        THROW 51114, 'Created principal does not match the approved direct Entra user.', 1;

    SET @Stage = 'P005'; -- Required-table inventory and existence check
    DECLARE @Grants nvarchar(max) = N'GRANT CONNECT TO ' + QUOTENAME(@ApprovedUpn) + N';';
    DECLARE @Tables TABLE (Name sysname PRIMARY KEY, InsertRequired bit NOT NULL);
    INSERT INTO @Tables VALUES
        (N'Incidents',1),(N'IncidentLocations',1),(N'IncidentStatusHistory',1),(N'AuditLogs',1),
        (N'IncidentTypes',0),(N'IncidentTypeResourceRequirements',0),(N'Departments',0),
        (N'AspNetUsers',0),(N'AspNetRoles',0),(N'Resources',0),(N'IncidentAssignments',0),
        (N'IncidentEscalations',0),(N'OperationalSituations',0);
    IF EXISTS (SELECT 1 FROM @Tables WHERE OBJECT_ID(N'dbo.' + Name, 'U') IS NULL)
        THROW 51115, 'Required seed or preview table missing; no schema changes will be attempted.', 1;
    SET @Stage = 'P006'; -- Permission-grant preparation and execution
    SELECT @Grants = @Grants + STRING_AGG(CONVERT(nvarchar(max), N'GRANT SELECT' +
        CASE WHEN InsertRequired = 1 THEN N', INSERT' ELSE N'' END +
        N' ON OBJECT::dbo.' + QUOTENAME(Name) + N' TO ' + QUOTENAME(@ApprovedUpn) + N';'), N'')
        WITHIN GROUP (ORDER BY Name) FROM @Tables;
    EXEC sys.sp_executesql @Grants;
    SET @Stage = 'P007'; -- Commit
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    -- Capture original numeric metadata before rollback can alter the context.
    DECLARE @FailedErrorNumber int = ERROR_NUMBER();
    DECLARE @FailedStage varchar(4) = @Stage;
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    -- Only an integer and a fixed stage enter the fixed failure message.
    DECLARE @FailureMessage nvarchar(2048) = N'Principal provisioning stopped. Stage=' +
        @FailedStage + N'; ErrorNumber=' + CONVERT(nvarchar(11), @FailedErrorNumber) +
        N'. No automatic retry.';
    THROW 51116, @FailureMessage, 1;
END CATCH;

SELECT name, type, DATALENGTH(sid) AS SidBytes,
       CONVERT(varchar(170), sid, 1) AS SqlSid
FROM sys.database_principals WHERE name = @ApprovedUpn;
-- No database role, ownership, broad schema grant, Azure RBAC or identity creation.
-- Provisioning completion is NOT seeding authorization. Reconnect as the exact
-- member user, inspect effective permissions, and run read-only hosted-preview.

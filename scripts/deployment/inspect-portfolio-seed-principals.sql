-- READ ONLY. Run in EmergencyResponse through an approved access path.
-- Administrator view: catalog visibility can hide other users/grants from a
-- limited principal. These catalogs alone do NOT prove another user's effective
-- permissions. Run the final sections again using the actual proposed identity.
SET NOCOUNT ON;
IF DB_NAME() <> N'EmergencyResponse'
    THROW 51100, 'Wrong database for portfolio principal inspection.', 1;

SELECT name, type, authentication_type_desc,
       DATALENGTH(sid) AS SidBytes, CONVERT(varchar(170), sid, 1) AS SqlSid
FROM sys.database_principals
WHERE type IN ('E', 'X') OR name = N'dbo'
ORDER BY name;

SELECT member.name AS MemberName, role.name AS DatabaseRole
FROM sys.database_role_members AS membership
JOIN sys.database_principals AS member ON member.principal_id = membership.member_principal_id
JOIN sys.database_principals AS role ON role.principal_id = membership.role_principal_id
WHERE member.type IN ('E', 'X')
ORDER BY member.name, role.name;

SELECT grantee.name AS Grantee, permission.state_desc, permission.permission_name,
       permission.class_desc,
       CASE WHEN permission.class = 1 THEN OBJECT_SCHEMA_NAME(permission.major_id) END AS ObjectSchema,
       CASE WHEN permission.class = 1 THEN OBJECT_NAME(permission.major_id)
            WHEN permission.class = 3 THEN SCHEMA_NAME(permission.major_id) END AS Securable
FROM sys.database_permissions AS permission
JOIN sys.database_principals AS grantee ON grantee.principal_id = permission.grantee_principal_id
WHERE grantee.type IN ('E', 'X') OR grantee.name = N'public'
ORDER BY grantee.name, permission.class, permission.major_id, permission.permission_name;

-- Session evidence: execute as the actual seeding user, NOT EXECUTE AS USER.
SELECT ORIGINAL_LOGIN() AS OriginalLogin, SUSER_SNAME() AS CurrentLogin,
       USER_NAME() AS DatabaseUser, DB_NAME() AS DatabaseName;
SELECT name, type, authentication_type_desc,
       DATALENGTH(sid) AS SidBytes, CONVERT(varchar(170), sid, 1) AS ApprovedSidCandidate,
       CASE WHEN type = 'E' AND authentication_type_desc = 'EXTERNAL'
            AND name = ORIGINAL_LOGIN() AND ORIGINAL_LOGIN() = SUSER_SNAME()
            AND DATALENGTH(sid) IN (16, 18) THEN 1 ELSE 0 END AS VerifierCompatible
FROM sys.database_principals WHERE principal_id = DATABASE_PRINCIPAL_ID();

-- Effective rights of THIS session, including inherited rights/denies.
-- RequiredSelect and RequiredInsert must be 1. NULL means unverifiable.
SELECT required.TableName, required.InsertRequired,
       HAS_PERMS_BY_NAME(N'dbo.' + required.TableName, 'OBJECT', 'SELECT') AS RequiredSelect,
       CASE WHEN required.InsertRequired = 1 THEN
           HAS_PERMS_BY_NAME(N'dbo.' + required.TableName, 'OBJECT', 'INSERT') END AS RequiredInsert
FROM (VALUES
    (N'Incidents', 1), (N'IncidentLocations', 1), (N'IncidentStatusHistory', 1), (N'AuditLogs', 1),
    (N'IncidentTypes', 0), (N'IncidentTypeResourceRequirements', 0), (N'Departments', 0),
    (N'AspNetUsers', 0), (N'AspNetRoles', 0), (N'Resources', 0), (N'IncidentAssignments', 0),
    (N'IncidentEscalations', 0), (N'OperationalSituations', 0)
) AS required(TableName, InsertRequired);

-- Inspect wider effective rights before approving least privilege. Expect
-- INSERT=0 outside the four seed tables, and UPDATE/DELETE/ALTER/CONTROL=0.
SELECT SCHEMA_NAME(t.schema_id) AS SchemaName, t.name AS TableName,
       HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name), 'OBJECT', 'INSERT') AS CanInsert,
       HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name), 'OBJECT', 'UPDATE') AS CanUpdate,
       HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name), 'OBJECT', 'DELETE') AS CanDelete,
       HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name), 'OBJECT', 'ALTER') AS CanAlter,
       HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name), 'OBJECT', 'CONTROL') AS CanControl
FROM sys.tables AS t WHERE t.is_ms_shipped = 0 ORDER BY SchemaName, TableName;
SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONNECT') AS CanConnect,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') AS HasDatabaseControl,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER') AS CanAlterDatabase,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') AS CanCreateTable,
       IS_ROLEMEMBER('db_owner') AS IsDatabaseOwner;
-- Metadata visibility may omit tables or inherited groups. An administrator
-- must compare the table inventory and group memberships before final approval.

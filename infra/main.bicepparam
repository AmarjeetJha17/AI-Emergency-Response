using './main.bicep'

param environmentName = 'staging'
// Supply these values through the local process environment for direct group-scope deployments.
// The staging what-if script resolves the active Entra user and passes them without writing a parameter file.
param sqlEntraAdminLogin = readEnvironmentVariable('ER_SQL_ENTRA_ADMIN_LOGIN')
param sqlEntraAdminObjectId = readEnvironmentVariable('ER_SQL_ENTRA_ADMIN_OBJECT_ID')
param sqlServerName = readEnvironmentVariable('ER_SQL_SERVER_NAME')

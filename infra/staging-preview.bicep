targetScope = 'subscription'

@description('Dedicated staging resource group name for the preview.')
param stagingResourceGroupName string = 'rg-er-staging-preview'

@description('Azure region for the staging resource group and resources.')
param location string

@description('Existing Azure AI resource group. Pass the value explicitly for each deployment.')
param aiResourceGroupName string

@description('Existing Azure AI account name. Pass the value explicitly for each deployment.')
param aiResourceName string

@description('Display name of the signed-in Microsoft Entra SQL administrator.')
param sqlEntraAdminLogin string

@description('Object ID of the signed-in Microsoft Entra SQL administrator.')
param sqlEntraAdminObjectId string

@description('SQL Database SKU used for the initial infrastructure preview; review cost and regional availability before deployment.')
param sqlDatabaseSkuName string = 'GP_S_Gen5_1'

var suffix = take(uniqueString(subscription().id, stagingResourceGroupName), 8)
var sqlServerName = 'sql-er-staging-${suffix}'

var commonTags = {
  project: 'AI-Emergency-Response'
  environment: 'staging'
  managedBy: 'Bicep'
}

resource stagingResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: stagingResourceGroupName
  location: location
  tags: commonTags
}

module stagingFoundation 'main.bicep' = {
  name: 'er-staging-network-foundation'
  scope: stagingResourceGroup
  params: {
    environmentName: 'staging'
    location: location
    aiResourceGroupName: aiResourceGroupName
    aiResourceName: aiResourceName
    sqlEntraAdminLogin: sqlEntraAdminLogin
    sqlEntraAdminObjectId: sqlEntraAdminObjectId
    sqlEntraAdminPrincipalType: 'User'
    sqlServerName: sqlServerName
    sqlDatabaseName: 'EmergencyResponse'
    sqlDatabaseSkuName: sqlDatabaseSkuName
  }
}

output stagingResourceGroupId string = stagingResourceGroup.id
output sqlServerName string = sqlServerName
output containerAppsEnvironmentName string = stagingFoundation.outputs.containerAppsEnvironmentName

targetScope = 'subscription'

@description('Dedicated resource group for the isolated synthetic portfolio demo.')
@minLength(1)
@maxLength(90)
param resourceGroupName string

@description('Azure region for the portfolio demo resources.')
param location string

@description('Non-overlapping address range for the isolated portfolio demo VNet.')
param vnetAddressPrefix string = '10.42.0.0/24'

@description('Dedicated ACA workload subnet inside vnetAddressPrefix; workload-profile environments require at least /27.')
param acaSubnetPrefix string = '10.42.0.0/27'

@description('Existing Azure AI resource group in this subscription. No AI account is created.')
param aiResourceGroupName string

@description('Existing Azure AI account name. No account or model deployment is created.')
param aiResourceName string

@description('Name of the existing chat deployment to use after its availability and permission are verified.')
param chatDeploymentName string = 'gpt-5-mini'

@description('Name of the existing embedding deployment to use after its availability and permission are verified.')
param embeddingDeploymentName string = 'text-embedding-3-small'

@description('Immutable container tag. Required even when app deployment is disabled.')
@minLength(7)
param imageTag string

@description('Microsoft Entra administrator display name for the Azure SQL server.')
param sqlEntraAdminLogin string

@description('Object ID of the Microsoft Entra SQL administrator.')
param sqlEntraAdminObjectId string

@description('Create/update the API container app after its image and Key Vault secrets exist.')
param deployApi bool = false

@description('Create/update the Web container app after its image and Key Vault secrets exist.')
param deployWeb bool = false

@description('Exact HTTPS Web origin to allow in API CORS. Keep empty until the Web FQDN is known.')
param webOrigin string = ''

@description('API HTTPS base URL for Web server-side and browser calls. Required when deployWeb is true.')
param apiBaseUrl string = ''

@description('Minimum API replicas. Zero lowers idle compute but process-local queued background work can be lost after scale-down.')
@minValue(0)
@maxValue(1)
param apiMinReplicas int = 0

@description('Grant the API managed identity the existing Azure AI account inference role. This role may appear as unsupported in what-if.')
param grantAiInferenceRole bool = false

@description('Mark model deployment availability verified only after manual deployment, permission, and inference checks.')
param aiDeploymentAvailabilityVerified bool = false

var portfolioTags = {
  project: 'AI-Emergency-Response'
  environment: 'portfolio-demo'
  managedBy: 'Bicep'
  workloadData: 'synthetic-only'
  costScope: 'intermittent-demo'
}

resource portfolioResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: portfolioTags
}

module portfolioDemo './modules/portfolio-demo-stack.bicep' = {
  name: 'portfolio-demo-stack'
  scope: portfolioResourceGroup
  params: {
    location: location
    vnetAddressPrefix: vnetAddressPrefix
    acaSubnetPrefix: acaSubnetPrefix
    aiResourceGroupName: aiResourceGroupName
    aiResourceName: aiResourceName
    chatDeploymentName: chatDeploymentName
    embeddingDeploymentName: embeddingDeploymentName
    imageTag: imageTag
    sqlEntraAdminLogin: sqlEntraAdminLogin
    sqlEntraAdminObjectId: sqlEntraAdminObjectId
    deployApi: deployApi
    deployWeb: deployWeb
    webOrigin: webOrigin
    apiBaseUrl: apiBaseUrl
    apiMinReplicas: apiMinReplicas
    grantAiInferenceRole: grantAiInferenceRole
    aiDeploymentAvailabilityVerified: aiDeploymentAvailabilityVerified
  }
}

output portfolioResourceGroupName string = portfolioResourceGroup.name
output resourceLocation string = portfolioResourceGroup.location
output apiManagedIdentityPrincipalId string = portfolioDemo.outputs.apiManagedIdentityPrincipalId
output sqlServerName string = portfolioDemo.outputs.sqlServerName
output sqlServerFqdn string = portfolioDemo.outputs.sqlServerFqdn
output sqlDatabaseName string = portfolioDemo.outputs.sqlDatabaseName
output containerRegistryName string = portfolioDemo.outputs.containerRegistryName
output apiContainerAppName string = portfolioDemo.outputs.apiContainerAppName
output apiContainerAppFqdn string = portfolioDemo.outputs.apiContainerAppFqdn
output webContainerAppName string = portfolioDemo.outputs.webContainerAppName
output webContainerAppFqdn string = portfolioDemo.outputs.webContainerAppFqdn
output keyVaultName string = portfolioDemo.outputs.keyVaultName
output storageAccountName string = portfolioDemo.outputs.storageAccountName

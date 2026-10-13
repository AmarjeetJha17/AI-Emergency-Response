targetScope = 'resourceGroup'

@description('Environment identifier used in resource names and tags.')
@allowed([
  'staging'
  'pilot'
])
param environmentName string = 'staging'

@description('Azure region. Defaults to the resource group region.')
param location string = resourceGroup().location

@description('Microsoft Entra administrator display name for Azure SQL.')
param sqlEntraAdminLogin string

@description('Object ID of the Microsoft Entra user or group administering Azure SQL.')
param sqlEntraAdminObjectId string

@description('Principal type of the Microsoft Entra administrator.')
@allowed([
  'User'
  'Group'
])
param sqlEntraAdminPrincipalType string = 'User'

@description('Globally unique Azure SQL logical server name.')
param sqlServerName string

@description('Azure SQL database name.')
param sqlDatabaseName string = 'EmergencyResponse'

@description('Initial Azure SQL database SKU for preview; confirm regional availability and cost before deployment.')
param sqlDatabaseSkuName string = 'GP_S_Gen5_1'

var suffix = take(uniqueString(resourceGroup().id), 8)

var commonTags = {
  project: 'AI-Emergency-Response'
  environment: environmentName
  managedBy: 'Bicep'
}

var logAnalyticsName = 'log-er-${environmentName}-${suffix}'
var containerAppsEnvironmentName = 'cae-er-${environmentName}-vnet'
var registryName = 'acer${environmentName}${suffix}'
var storageAccountName = 'ster${environmentName}${suffix}'
var keyVaultName = 'kv-er-${environmentName}-${suffix}'
var serviceBusName = 'sb-er-${environmentName}-${suffix}'
var vnetName = 'vnet-er-${environmentName}'

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  tags: commonTags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.40.0.0/16'
      ]
    }
  }
}

resource acaSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: vnet
  name: 'aca-infrastructure'
  properties: {
    addressPrefix: '10.40.0.0/23'
    delegations: [
      {
        name: 'aca-delegation'
        properties: {
          serviceName: 'Microsoft.App/environments'
        }
      }
    ]
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: vnet
  name: 'private-endpoints'
  properties: {
    addressPrefix: '10.40.2.0/24'
    privateEndpointNetworkPolicies: 'Disabled'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: commonTags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  tags: commonTags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: acaSubnet.id
      internal: false
    }
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  tags: commonTags
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource acrPullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-er-${environmentName}-acr-pull'
  location: location
  tags: commonTags
}

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, acrPullIdentity.id, 'AcrPull')
  scope: registry
  properties: {
    principalId: acrPullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    )
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: commonTags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    isVersioningEnabled: true
  }
}

resource incidentMediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'incident-media'
  properties: {
    publicAccess: 'None'
  }
}

resource knowledgeDocumentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'knowledge-documents'
  properties: {
    publicAccess: 'None'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: commonTags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusName
  location: location
  tags: commonTags
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 0
  }
  properties: {
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
    zoneRedundant: false
  }
}

resource incidentAiQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' = {
  parent: serviceBus
  name: 'incident-ai'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    enableExpress: false
    enablePartitioning: true
    maxDeliveryCount: 10
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: true
    requiresSession: false
    status: 'Active'
  }
}

resource visualAnalysisQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' = {
  parent: serviceBus
  name: 'visual-analysis'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    enableExpress: false
    enablePartitioning: true
    maxDeliveryCount: 10
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: true
    requiresSession: false
    status: 'Active'
  }
}

resource duplicateDetectionQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' = {
  parent: serviceBus
  name: 'duplicate-detection'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    enableExpress: false
    enablePartitioning: true
    maxDeliveryCount: 10
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: true
    requiresSession: false
    status: 'Active'
  }
}

resource externalContextQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' = {
  parent: serviceBus
  name: 'external-context'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    enableExpress: false
    enablePartitioning: true
    maxDeliveryCount: 10
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: true
    requiresSession: false
    status: 'Active'
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  tags: commonTags
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: sqlEntraAdminLogin
      principalType: sqlEntraAdminPrincipalType
      sid: sqlEntraAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
  }
}

resource sqlEntraOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2024-11-01-preview' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: commonTags
  sku: {
    name: sqlDatabaseSkuName
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368
    zoneRedundant: false
  }
  dependsOn: [
    sqlEntraOnlyAuth
  ]
}

resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  #disable-next-line no-hardcoded-env-urls
  name: 'privatelink.database.windows.net'
  location: 'global'
  tags: commonTags
}

resource sqlPrivateDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: sqlPrivateDnsZone
  name: '${vnetName}-sql-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-sql-er-${environmentName}'
  location: location
  tags: commonTags
  properties: {
    subnet: {
      id: privateEndpointSubnet.id
    }
    privateLinkServiceConnections: [
      {
        name: 'sql-server-connection'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
          ]
          requestMessage: 'Private SQL connectivity for the emergency response application.'
        }
      }
    ]
  }
  dependsOn: [
    sqlDatabase
  ]
}

resource sqlPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: sqlPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'sql-private-dns'
        properties: {
          privateDnsZoneId: sqlPrivateDnsZone.id
        }
      }
    ]
  }
}

output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output registryName string = registry.name
output acrPullIdentityName string = acrPullIdentity.name
output storageAccountName string = storage.name
output mediaContainerName string = incidentMediaContainer.name
output knowledgeContainerName string = knowledgeDocumentsContainer.name
output keyVaultName string = keyVault.name
output serviceBusNamespaceName string = serviceBus.name
output logAnalyticsWorkspaceName string = logAnalytics.name
output incidentAiQueueName string = incidentAiQueue.name
output visualAnalysisQueueName string = visualAnalysisQueue.name
output duplicateDetectionQueueName string = duplicateDetectionQueue.name
output externalContextQueueName string = externalContextQueue.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseNameOutput string = sqlDatabase.name
output privateVnetId string = vnet.id
output sqlPrivateEndpointId string = sqlPrivateEndpoint.id

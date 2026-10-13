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

@description('Resource group containing the existing Foundry/Azure OpenAI resource.')
param aiResourceGroupName string = 'AI-Emergency-Response-RG'

@description('Existing Azure AI account used by low-volume synthetic staging calls.')
param aiResourceName string = 'ai-emergency-response'

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
var searchServiceName = 'srcher${environmentName}${suffix}'
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

resource migrationRunnerSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: vnet
  name: 'migration-runner'
  properties: {
    addressPrefix: '10.40.3.0/24'
  }
}

resource bastionSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: vnet
  name: 'AzureBastionSubnet'
  properties: {
    addressPrefix: '10.40.4.0/26'
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

resource apiRuntimeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-er-${environmentName}-api'
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
    publicNetworkAccess: 'Disabled'
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

resource dataProtectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'web-data-protection'
  properties: {
    publicAccess: 'None'
  }
}

resource deploymentArtifactsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'deployment-artifacts'
  properties: {
    publicAccess: 'None'
  }
}

resource blobPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  #disable-next-line no-hardcoded-env-urls
  name: 'privatelink.blob.core.windows.net'
  location: 'global'
  tags: commonTags
}

resource blobPrivateDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: blobPrivateDnsZone
  name: '${vnetName}-blob-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource blobPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-blob-er-${environmentName}'
  location: location
  tags: commonTags
  properties: {
    subnet: {
      id: privateEndpointSubnet.id
    }
    privateLinkServiceConnections: [
      {
        name: 'blob-storage-connection'
        properties: {
          privateLinkServiceId: storage.id
          groupIds: [
            'blob'
          ]
          requestMessage: 'Private incident and knowledge file storage.'
        }
      }
    ]
  }
}

resource blobPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: blobPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'blob-private-dns'
        properties: {
          privateDnsZoneId: blobPrivateDnsZone.id
        }
      }
    ]
  }
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchServiceName
  location: location
  tags: commonTags
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'disabled'
    semanticSearch: 'disabled'
  }
}

resource searchPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  #disable-next-line no-hardcoded-env-urls
  name: 'privatelink.search.windows.net'
  location: 'global'
  tags: commonTags
}

resource searchPrivateDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: searchPrivateDnsZone
  name: '${vnetName}-search-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource searchPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-search-er-${environmentName}'
  location: location
  tags: commonTags
  properties: {
    subnet: {
      id: privateEndpointSubnet.id
    }
    privateLinkServiceConnections: [
      {
        name: 'search-service-connection'
        properties: {
          privateLinkServiceId: searchService.id
          groupIds: [
            'searchService'
          ]
          requestMessage: 'Private staging knowledge search.'
        }
      }
    ]
  }
}

resource searchPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: searchPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'search-private-dns'
        properties: {
          privateDnsZoneId: searchPrivateDnsZone.id
        }
      }
    ]
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

resource webDataProtectionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'web-data-protection'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'encrypt'
      'decrypt'
      'wrapKey'
      'unwrapKey'
      'sign'
      'verify'
    ]
    attributes: {
      enabled: true
    }
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

resource apiIncidentMediaRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(incidentMediaContainer.id, apiRuntimeIdentity.id, 'Storage Blob Data Contributor')
  scope: incidentMediaContainer
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource apiKnowledgeBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(knowledgeDocumentsContainer.id, apiRuntimeIdentity.id, 'Storage Blob Data Contributor')
  scope: knowledgeDocumentsContainer
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource apiSearchRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, apiRuntimeIdentity.id, 'Search Index Data Contributor')
  scope: searchService
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
  }
}

resource apiKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiRuntimeIdentity.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource apiIncidentAiSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(incidentAiQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Sender')
  scope: incidentAiQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  }
}

resource apiVisualSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(visualAnalysisQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Sender')
  scope: visualAnalysisQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  }
}

resource apiDuplicateSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(duplicateDetectionQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Sender')
  scope: duplicateDetectionQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  }
}

resource apiExternalContextSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(externalContextQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Sender')
  scope: externalContextQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  }
}

resource apiIncidentAiReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(incidentAiQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Receiver')
  scope: incidentAiQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9146-0e5a2a2247e0')
  }
}

resource apiVisualReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(visualAnalysisQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Receiver')
  scope: visualAnalysisQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9146-0e5a2a2247e0')
  }
}

resource apiDuplicateReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(duplicateDetectionQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Receiver')
  scope: duplicateDetectionQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9146-0e5a2a2247e0')
  }
}

resource apiExternalContextReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(externalContextQueue.id, apiRuntimeIdentity.id, 'Azure Service Bus Data Receiver')
  scope: externalContextQueue
  properties: {
    principalId: apiRuntimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9146-0e5a2a2247e0')
  }
}

module apiAiRole 'modules/ai-role.bicep' = {
  name: 'er-staging-api-ai-role'
  scope: resourceGroup(aiResourceGroupName)
  params: {
    aiResourceName: aiResourceName
    apiPrincipalId: apiRuntimeIdentity.properties.principalId
  }
}

output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output registryName string = registry.name
output acrPullIdentityName string = acrPullIdentity.name
output apiRuntimeIdentityName string = apiRuntimeIdentity.name
output apiRuntimeIdentityClientId string = apiRuntimeIdentity.properties.clientId
output apiRuntimeIdentityPrincipalId string = apiRuntimeIdentity.properties.principalId
output storageAccountName string = storage.name
output mediaContainerName string = incidentMediaContainer.name
output knowledgeContainerName string = knowledgeDocumentsContainer.name
output dataProtectionContainerName string = dataProtectionContainer.name
output deploymentArtifactsContainerName string = deploymentArtifactsContainer.name
output blobServiceUri string = 'https://${storage.name}.${environment().suffixes.storage}'
output searchServiceName string = searchService.name
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchIndexName string = 'emergency-knowledge-staging'
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output webDataProtectionKeyUri string = webDataProtectionKey.properties.keyUriWithVersion
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
output blobPrivateEndpointId string = blobPrivateEndpoint.id
output searchPrivateEndpointId string = searchPrivateEndpoint.id

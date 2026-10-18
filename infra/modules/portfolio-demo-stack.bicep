targetScope = 'resourceGroup'

@description('Azure region for the portfolio demo resources.')
param location string = resourceGroup().location

@description('Non-overlapping address range for the isolated portfolio demo VNet.')
param vnetAddressPrefix string = '10.42.0.0/24'

@description('Dedicated ACA workload subnet inside vnetAddressPrefix; workload-profile environments require at least /27.')
param acaSubnetPrefix string = '10.42.0.0/27'

@description('Resource group containing the existing Azure AI account.')
param aiResourceGroupName string

@description('Existing Azure AI account name. This module never creates or updates the account.')
param aiResourceName string

@description('Name of the existing chat deployment to use after verification.')
param chatDeploymentName string = 'gpt-5-mini'

@description('Name of the existing embedding deployment to use after verification.')
param embeddingDeploymentName string = 'text-embedding-3-small'

@description('Immutable image tag to deploy when either app toggle is enabled.')
@minLength(7)
param imageTag string

@description('Microsoft Entra administrator display name for Azure SQL.')
param sqlEntraAdminLogin string

@description('Object ID of the Microsoft Entra SQL administrator.')
param sqlEntraAdminObjectId string

param deployApi bool = false
param deployWeb bool = false
param webOrigin string = ''
param apiBaseUrl string = ''

@minValue(0)
@maxValue(1)
param apiMinReplicas int = 0

param grantAiInferenceRole bool = false
param aiDeploymentAvailabilityVerified bool = false

var suffix = take(uniqueString(subscription().id, resourceGroup().id), 8)
var tags = {
  project: 'AI-Emergency-Response'
  environment: 'portfolio-demo'
  managedBy: 'Bicep'
  workloadData: 'synthetic-only'
  costScope: 'intermittent-demo'
}
var vnetName = 'vnet-er-portfolio-demo'
var acaSubnetName = 'aca-infrastructure'
var logAnalyticsName = 'log-er-portfolio-${suffix}'
var environmentName = 'cae-er-portfolio-demo'
var registryName = 'acerportfolio${suffix}'
var storageName = 'sterportfolio${suffix}'
var keyVaultName = 'kv-er-portfolio-${suffix}'
var sqlServerName = 'sql-er-demo-${suffix}'
var sqlDatabaseName = 'EmergencyResponse'
var apiAppName = 'ca-er-api-portfolio'
var webAppName = 'ca-er-web-portfolio'
var apiIdentityName = 'id-er-portfolio-api'
var webIdentityName = 'id-er-portfolio-web'

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressPrefix
      ]
    }
  }
}

resource acaSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: vnet
  name: acaSubnetName
  properties: {
    addressPrefix: acaSubnetPrefix
    delegations: [
      {
        name: 'aca-delegation'
        properties: {
          serviceName: 'Microsoft.App/environments'
        }
      }
    ]
    serviceEndpoints: [
      {
        service: 'Microsoft.Sql'
      }
      {
        service: 'Microsoft.Storage'
      }
      {
        service: 'Microsoft.KeyVault'
      }
    ]
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
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
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    vnetConfiguration: {
      infrastructureSubnetId: acaSubnet.id
      internal: false
    }
    zoneRedundant: false
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: apiIdentityName
  location: location
  tags: tags
}

resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: webIdentityName
  location: location
  tags: tags
}

resource apiAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, apiIdentity.id, 'AcrPull')
  scope: registry
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource webAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, webIdentity.id, 'AcrPull')
  scope: registry
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  tags: tags
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
    networkAcls: {
      bypass: 'None'
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: acaSubnet.id
          action: 'Allow'
        }
      ]
    }
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
    isVersioningEnabled: false
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

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
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
    networkAcls: {
      bypass: 'None'
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: acaSubnet.id
        }
      ]
    }
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

resource apiKeyVaultSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiIdentity.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource webKeyVaultSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webIdentity.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource webKeyVaultCryptoRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webIdentity.id, 'Key Vault Crypto User')
  scope: keyVault
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'e147488a-f6f5-4113-8e2d-b22465e65bf6')
  }
}

resource apiMediaRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(incidentMediaContainer.id, apiIdentity.id, 'Storage Blob Data Contributor')
  scope: incidentMediaContainer
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource apiKnowledgeRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(knowledgeDocumentsContainer.id, apiIdentity.id, 'Storage Blob Data Contributor')
  scope: knowledgeDocumentsContainer
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource webDataProtectionRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(dataProtectionContainer.id, webIdentity.id, 'Storage Blob Data Contributor')
  scope: dataProtectionContainer
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: sqlEntraAdminLogin
      principalType: 'User'
      sid: sqlEntraAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
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
  tags: tags
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368
    zoneRedundant: false
    autoPauseDelay: 60
    minCapacity: json('0.5')
  }
  dependsOn: [
    sqlEntraOnlyAuth
  ]
}

resource sqlVirtualNetworkRule 'Microsoft.Sql/servers/virtualNetworkRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'portfolio-demo-container-apps'
  properties: {
    ignoreMissingVnetServiceEndpoint: false
    virtualNetworkSubnetId: acaSubnet.id
  }
  dependsOn: [
    sqlDatabase
  ]
}

resource aiAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: aiResourceName
  scope: resourceGroup(aiResourceGroupName)
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = if (deployApi) {
  name: apiAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registry.properties.loginServer
          identity: apiIdentity.id
        }
      ]
      secrets: [
        {
          name: 'jwt-signing-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/jwt-signing-key'
          identity: apiIdentity.id
        }
        {
          name: 'admin-seed-password'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/admin-seed-password'
          identity: apiIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      revisionSuffix: imageTag
      containers: [
        {
          name: 'api'
          image: '${registry.properties.loginServer}/emergencyresponse-api:${imageTag}'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: concat([
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Staging' }
            { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
            { name: 'AZURE_CLIENT_ID', value: apiIdentity.properties.clientId }
            { name: 'ConnectionStrings__DefaultConnection', value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;' }
            { name: 'Jwt__Issuer', value: 'EmergencyResponse.Api' }
            { name: 'Jwt__Audience', value: 'EmergencyResponse.Client' }
            { name: 'Jwt__ExpirationMinutes', value: '60' }
            { name: 'Jwt__SecretKey', secretRef: 'jwt-signing-key' }
            { name: 'AdminSeed__Email', value: 'portfolio-admin@emergencyresponse.example' }
            { name: 'AdminSeed__FirstName', value: 'Portfolio' }
            { name: 'AdminSeed__LastName', value: 'Administrator' }
            { name: 'AdminSeed__Password', secretRef: 'admin-seed-password' }
            { name: 'Storage__BlobServiceUri', value: 'https://${storage.name}.${environment().suffixes.storage}' }
            { name: 'Storage__IncidentMediaContainer', value: incidentMediaContainer.name }
            { name: 'Storage__KnowledgeContainer', value: knowledgeDocumentsContainer.name }
            { name: 'AI__Endpoint', value: aiAccount.properties.endpoint }
            { name: 'AI__Deployment', value: chatDeploymentName }
            { name: 'AI__Model', value: chatDeploymentName }
            { name: 'AI__EmbeddingDeployment', value: embeddingDeploymentName }
            { name: 'AI__DeploymentAvailabilityVerified', value: string(aiDeploymentAvailabilityVerified) }
            { name: 'KnowledgeSearch__Endpoint', value: '' }
            { name: 'KnowledgeSearch__IndexName', value: 'portfolio-demo-disabled' }
          ], empty(webOrigin) ? [] : [
            { name: 'Cors__AllowedOrigins__0', value: webOrigin }
          ])
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 8080, scheme: 'HTTP' }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: apiMinReplicas
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    apiAcrPullRole
    apiKeyVaultSecretsRole
    apiMediaRole
    apiKnowledgeRole
  ]
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = if (deployWeb) {
  name: webAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${webIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registry.properties.loginServer
          identity: webIdentity.id
        }
      ]
      secrets: [
        {
          name: 'syncfusion-license-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/syncfusion-license-key'
          identity: webIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      revisionSuffix: imageTag
      containers: [
        {
          name: 'web'
          image: '${registry.properties.loginServer}/emergencyresponse-web:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Staging' }
            { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
            { name: 'AZURE_CLIENT_ID', value: webIdentity.properties.clientId }
            { name: 'ApiSettings__BaseUrl', value: apiBaseUrl }
            { name: 'Syncfusion__LicenseKey', secretRef: 'syncfusion-license-key' }
            { name: 'DataProtection__BlobUri', value: 'https://${storage.name}.${environment().suffixes.storage}/${dataProtectionContainer.name}/keys.xml' }
            { name: 'DataProtection__KeyIdentifier', value: webDataProtectionKey.properties.keyUriWithVersion }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 8080, scheme: 'HTTP' }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    webAcrPullRole
    webKeyVaultSecretsRole
    webKeyVaultCryptoRole
    webDataProtectionRole
  ]
}

module apiAiRole './ai-role.bicep' = if (grantAiInferenceRole) {
  name: 'portfolio-demo-api-ai-role'
  scope: resourceGroup(aiResourceGroupName)
  params: {
    aiResourceName: aiResourceName
    apiPrincipalId: apiIdentity.properties.principalId
  }
}

output apiManagedIdentityPrincipalId string = apiIdentity.properties.principalId
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output containerRegistryName string = registry.name
output apiContainerAppName string = deployApi ? apiApp!.name : apiAppName
output apiContainerAppFqdn string = deployApi ? apiApp!.properties.configuration.ingress.fqdn : ''
output webContainerAppName string = deployWeb ? webApp!.name : webAppName
output webContainerAppFqdn string = deployWeb ? webApp!.properties.configuration.ingress.fqdn : ''
output keyVaultName string = keyVault.name
output storageAccountName string = storage.name

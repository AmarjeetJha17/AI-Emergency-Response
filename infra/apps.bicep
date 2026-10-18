targetScope = 'resourceGroup'

@description('Staging Container Apps environment name from main.bicep.')
param environmentName string = 'staging'

@description('Immutable image tag, preferably the 12-character source commit SHA.')
@minLength(7)
param imageTag string

@description('Deploy the Web app after the API URL is known.')
param deployWeb bool = false

@description('API base URL used by the Web server, for example https://<api-fqdn>/. Required when deployWeb is true.')
param apiBaseUrl string = ''

@description('HTTPS Web origin allowed by API CORS. Supply the deployed Web origin on the second pass.')
param webOrigin string = ''

@description('Whether API ingress is externally reachable. Keep true while the browser connects directly to its SignalR hub.')
param apiExternalIngress bool = true

@description('Synthetic staging system administrator email.')
param adminSeedEmail string = 'pilot-admin@emergencyresponse.example'

@description('Resource group containing the existing Foundry/Azure OpenAI resource.')
param aiResourceGroupName string

@description('Existing Azure AI resource name used by the API.')
param aiResourceName string

var suffix = take(uniqueString(resourceGroup().id), 8)
var containerAppsEnvironmentName = 'cae-er-${environmentName}-vnet'
var registryName = 'acer${environmentName}${suffix}'
var storageAccountName = 'ster${environmentName}${suffix}'
var keyVaultName = 'kv-er-${environmentName}-${suffix}'
var serviceBusName = 'sb-er-${environmentName}-${suffix}'
var searchServiceName = 'srcher${environmentName}${suffix}'
var sqlServerName = 'sql-er-staging-${take(uniqueString(subscription().id, resourceGroup().name), 8)}'
var sqlDatabaseName = 'EmergencyResponse'
var apiAppName = 'ca-er-api-${environmentName}'
var webAppName = 'ca-er-web-${environmentName}'

var commonTags = {
  project: 'AI-Emergency-Response'
  environment: environmentName
  managedBy: 'Bicep'
  workloadData: 'synthetic-only'
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppsEnvironmentName
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

resource acrPullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: 'id-er-${environmentName}-acr-pull'
}

resource apiRuntimeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: 'id-er-${environmentName}-api'
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource incidentMediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobService
  name: 'incident-media'
}

resource knowledgeDocumentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobService
  name: 'knowledge-documents'
}

resource dataProtectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobService
  name: 'web-data-protection'
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource webDataProtectionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' existing = {
  parent: keyVault
  name: 'web-data-protection'
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
}

resource incidentAiQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' existing = {
  parent: serviceBus
  name: 'incident-ai'
}

resource visualAnalysisQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' existing = {
  parent: serviceBus
  name: 'visual-analysis'
}

resource duplicateDetectionQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' existing = {
  parent: serviceBus
  name: 'duplicate-detection'
}

resource externalContextQueue 'Microsoft.ServiceBus/namespaces/queues@2021-06-01-preview' existing = {
  parent: serviceBus
  name: 'external-context'
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: searchServiceName
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: sqlServerName
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' existing = {
  parent: sqlServer
  name: sqlDatabaseName
}

resource aiAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: aiResourceName
  scope: resourceGroup(aiResourceGroupName)
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: resourceGroup().location
  tags: commonTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {}
      '${apiRuntimeIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registry.properties.loginServer
          identity: acrPullIdentity.id
        }
      ]
      secrets: [
        {
          name: 'jwt-signing-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/jwt-signing-key'
          identity: apiRuntimeIdentity.id
        }
        {
          name: 'admin-seed-password'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/admin-seed-password'
          identity: apiRuntimeIdentity.id
        }
      ]
      ingress: {
        external: apiExternalIngress
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
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Staging'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: apiRuntimeIdentity.properties.clientId
            }
            {
              name: 'ASPNETCORE_HTTP_PORTS'
              value: '8080'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
            }
            {
              name: 'Jwt__Issuer'
              value: 'EmergencyResponse.Api'
            }
            {
              name: 'Jwt__Audience'
              value: 'EmergencyResponse.Client'
            }
            {
              name: 'Jwt__ExpirationMinutes'
              value: '60'
            }
            {
              name: 'Jwt__SecretKey'
              secretRef: 'jwt-signing-key'
            }
            {
              name: 'AdminSeed__Email'
              value: adminSeedEmail
            }
            {
              name: 'AdminSeed__FirstName'
              value: 'Staging'
            }
            {
              name: 'AdminSeed__LastName'
              value: 'Administrator'
            }
            {
              name: 'AdminSeed__Password'
              secretRef: 'admin-seed-password'
            }
            {
              name: 'Messaging__Provider'
              value: 'AzureServiceBus'
            }
            {
              name: 'Messaging__ServiceBus__FullyQualifiedNamespace'
              value: '${serviceBus.name}.servicebus.windows.net'
            }
            {
              name: 'Storage__BlobServiceUri'
              value: 'https://${storage.name}.${environment().suffixes.storage}'
            }
            {
              name: 'Storage__IncidentMediaContainer'
              value: incidentMediaContainer.name
            }
            {
              name: 'Storage__KnowledgeContainer'
              value: knowledgeDocumentsContainer.name
            }
            {
              name: 'AI__Endpoint'
              value: aiAccount.properties.endpoint
            }
            {
              name: 'AI__Deployment'
              value: 'gpt-5-mini'
            }
            {
              name: 'AI__Model'
              value: 'gpt-5-mini'
            }
            {
              name: 'AI__EmbeddingDeployment'
              value: 'text-embedding-3-small'
            }
            {
              name: 'KnowledgeSearch__Endpoint'
              value: 'https://${searchService.name}.search.windows.net'
            }
            {
              name: 'KnowledgeSearch__IndexName'
              value: 'emergency-knowledge-staging'
            }
            {
              name: 'KnowledgeSearch__CreateIndexIfMissing'
              value: 'false'
            }
          ], empty(webOrigin) ? [] : [
            {
              name: 'Cors__AllowedOrigins__0'
              value: webOrigin
            }
          ])
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = if (deployWeb) {
  name: webAppName
  location: resourceGroup().location
  tags: commonTags
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registry.properties.loginServer
          identity: acrPullIdentity.id
        }
      ]
      secrets: [
        {
          name: 'syncfusion-license-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/syncfusion-license-key'
          identity: 'system'
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
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Staging'
            }
            {
              name: 'ASPNETCORE_HTTP_PORTS'
              value: '8080'
            }
            {
              name: 'ApiSettings__BaseUrl'
              value: apiBaseUrl
            }
            {
              name: 'Syncfusion__LicenseKey'
              secretRef: 'syncfusion-license-key'
            }
            {
              name: 'DataProtection__BlobUri'
              value: 'https://${storage.name}.${environment().suffixes.storage}/${dataProtectionContainer.name}/keys.xml'
            }
            {
              name: 'DataProtection__KeyIdentifier'
              value: webDataProtectionKey.properties.keyUriWithVersion
            }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
  dependsOn: [
    apiApp
  ]
}

resource apiMediaRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
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

resource webDataProtectionBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployWeb) {
  name: guid(dataProtectionContainer.id, webApp.id, 'Storage Blob Data Contributor')
  scope: dataProtectionContainer
  properties: {
    principalId: webApp!.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource webKeyVaultSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployWeb) {
  name: guid(keyVault.id, webApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: webApp!.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource webKeyVaultCryptoRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployWeb) {
  name: guid(keyVault.id, webApp.id, 'Key Vault Crypto User')
  scope: keyVault
  properties: {
    principalId: webApp!.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'e147488a-f6f5-4113-8e2d-b22465e65bf6')
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

output apiAppName string = apiApp.name
output apiFqdn string = apiApp.properties.configuration.ingress.fqdn
output apiManagedIdentityPrincipalId string = apiRuntimeIdentity.properties.principalId
output webAppName string = deployWeb ? webApp!.name : ''
output webFqdn string = deployWeb ? webApp!.properties.configuration.ingress.fqdn : ''
output webManagedIdentityPrincipalId string = deployWeb ? webApp!.identity.principalId : ''

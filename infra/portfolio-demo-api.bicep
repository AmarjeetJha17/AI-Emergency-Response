targetScope = 'resourceGroup'

@description('Immutable container image tag already pushed to the existing portfolio ACR.')
@allowed([
  'portfolio-20261024-200121-sqlclient711'
])
@minLength(7)
param imageTag string

@description('Unique suffix for the API revision created by a native-secret update.')
@minLength(1)
@maxLength(63)
param revisionSuffix string

@description('Azure region for the API Container App.')
param location string = 'centralus'

@description('Optional browser origin allowed by the API CORS policy.')
param webOrigin string = ''

@description('Portfolio-only JWT HMAC signing key. Supply at deployment time through a secure parameter channel; use at least 32 random ASCII bytes.')
@secure()
@minLength(32)
param apiJwtSigningKey string

@description('Password for the initial synthetic portfolio administrator. It is used only when that email does not already exist.')
@secure()
@minLength(12)
param apiAdminSeedPassword string

@description('Minimum API replicas for the intermittent portfolio demo.')
@minValue(0)
@maxValue(1)
param apiMinReplicas int = 0

var apiAppName = 'ca-er-api-portfolio'
var sqlDatabaseName = 'EmergencyResponse'
var apiTags = {
  project: 'AI-Emergency-Response'
  environment: 'portfolio-demo'
  managedBy: 'Bicep'
  workloadData: 'synthetic-only'
  costScope: 'intermittent-demo'
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: 'cae-er-portfolio-demo'
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: 'acerportfoliobxwiyolb'
  scope: resourceGroup('rg-er-portfolio-demo-eastus2')
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: 'id-er-portfolio-api'
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: 'sterportfolioqqgqkekj'
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

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: 'sql-er-demo-qqgqkekj'
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' existing = {
  parent: sqlServer
  name: sqlDatabaseName
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  tags: apiTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      maxInactiveRevisions: 100
      registries: [
        {
          server: registry.properties.loginServer
          identity: apiIdentity.id
        }
      ]
      secrets: [
        {
          name: 'jwt-signing-key'
          value: apiJwtSigningKey
        }
        {
          name: 'admin-seed-password'
          value: apiAdminSeedPassword
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        exposedPort: 0
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
      revisionSuffix: revisionSuffix
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
            { name: 'AI__Enabled', value: 'false' }
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
          ], empty(webOrigin) ? [] : [
            { name: 'Cors__AllowedOrigins__0', value: webOrigin }
          ])
          probes: [
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
            {
              type: 'Startup'
              httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 30
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
}

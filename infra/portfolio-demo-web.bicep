targetScope = 'resourceGroup'

@description('Unique revision suffix for the previewed Web revision.')
@minLength(1)
@maxLength(64)
param revisionSuffix string

@description('Syncfusion license key supplied only through a secure parameter channel when deployment is explicitly approved.')
@secure()
@minLength(1)
param syncfusionLicenseKey string

var appName = 'ca-er-web-portfolio'
var appLocation = 'centralus'
var registryName = 'acerportfoliobxwiyolb'
var image = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b'
var apiBaseUrl = 'https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io'
// This portfolio target is Azure public cloud; use the existing account/container resources rather than literals for the path.
#disable-next-line no-hardcoded-env-urls
var dataProtectionBlobUri = 'https://${storage.name}.blob.core.windows.net/${dataProtectionContainer.name}/keys.xml'

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: 'cae-er-portfolio-demo'
}
resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: 'id-er-portfolio-web'
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: 'sterportfolioqqgqkekj'
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource dataProtectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobService
  name: 'web-data-protection'
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
  scope: resourceGroup('rg-er-portfolio-demo-eastus2')
}

// PortfolioDemo deliberately persists synthetic-demo Data Protection keys in Blob Storage without Key Vault wrapping.
// Never use this fallback for production data; DataProtection__KeyIdentifier is intentionally absent.
resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: appLocation
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${webIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: environment.id
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
          value: syncfusionLicenseKey
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
      revisionSuffix: revisionSuffix
      containers: [
        {
          name: 'web'
          image: image
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'PortfolioDemo' }
            { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
            { name: 'AZURE_CLIENT_ID', value: webIdentity.properties.clientId }
            { name: 'ApiSettings__BaseUrl', value: apiBaseUrl }
            { name: 'Syncfusion__LicenseKey', secretRef: 'syncfusion-license-key' }
            { name: 'DataProtection__BlobUri', value: dataProtectionBlobUri }
            { name: 'DataProtection__UsePortfolioDemoUnwrappedKeys', value: 'true' }
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
}

\n
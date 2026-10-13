targetScope = 'resourceGroup'

@description('Temporary runner name. Delete this deployment after migration and Search index setup.')
param runnerName string = 'vm-er-staging-migration'

@description('SSH public key for the temporary Linux runner. Never pass a private key.')
@minLength(64)
param sshPublicKey string

@description('Azure region for the runner and Bastion.')
param location string = resourceGroup().location

@description('Linux SSH user on the temporary migration runner.')
param adminUsername string = 'stagingops'

var environmentName = 'staging'
var suffix = take(uniqueString(resourceGroup().id), 8)
var vnetName = 'vnet-er-${environmentName}'
var storageAccountName = 'ster${environmentName}${suffix}'
var searchServiceName = 'srcher${environmentName}${suffix}'
var sqlServerName = 'sql-er-staging-${take(uniqueString(subscription().id, resourceGroup().name), 8)}'

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' existing = {
  name: vnetName
}

resource runnerSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' existing = {
  parent: vnet
  name: 'migration-runner'
}

resource bastionSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' existing = {
  parent: vnet
  name: 'AzureBastionSubnet'
}

resource runnerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-er-staging-migration'
  location: location
  tags: {
    project: 'AI-Emergency-Response'
    environment: environmentName
    purpose: 'temporary-staging-migration'
  }
}

resource runnerNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-er-staging-migration'
  location: location
  properties: {
    securityRules: [
      {
        name: 'allow-ssh-from-bastion'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: '10.40.4.0/26'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'deny-other-inbound'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource runnerNic 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: '${runnerName}-nic'
  location: location
  properties: {
    networkSecurityGroup: {
      id: runnerNsg.id
    }
    ipConfigurations: [
      {
        name: 'private-ip'
        properties: {
          subnet: {
            id: runnerSubnet.id
          }
          privateIPAllocationMethod: 'Dynamic'
        }
      }
    ]
  }
}

resource runnerVm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: runnerName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${runnerIdentity.id}': {}
    }
  }
  properties: {
    hardwareProfile: {
      vmSize: 'Standard_B2s'
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        deleteOption: 'Delete'
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
      }
    }
    osProfile: {
      computerName: runnerName
      adminUsername: adminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: runnerNic.id
          properties: {
            primary: true
            deleteOption: 'Delete'
          }
        }
      ]
    }
  }
  tags: {
    project: 'AI-Emergency-Response'
    environment: environmentName
    purpose: 'temporary-staging-migration-delete-after-use'
  }
}

resource bastionPublicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: 'pip-er-staging-bastion'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource bastion 'Microsoft.Network/bastionHosts@2024-05-01' = {
  name: 'bas-er-staging'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    enableTunneling: true
    ipConfigurations: [
      {
        name: 'bastion-ip-configuration'
        properties: {
          subnet: {
            id: bastionSubnet.id
          }
          publicIPAddress: {
            id: bastionPublicIp.id
          }
        }
      }
    ]
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource artifactsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobService
  name: 'deployment-artifacts'
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: searchServiceName
}

resource runnerSearchRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, runnerIdentity.id, 'Search Index Data Contributor')
  scope: searchService
  properties: {
    principalId: runnerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
  }
}

output runnerName string = runnerVm.name
output runnerIdentityName string = runnerIdentity.name
output runnerIdentityClientId string = runnerIdentity.properties.clientId
output bastionName string = bastion.name
output runnerPrivateIp string = runnerNic.properties.ipConfigurations[0].properties.privateIPAddress
output storageAccountName string = storage.name
output artifactContainerName string = artifactsContainer.name
output sqlServerName string = sqlServerName
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'

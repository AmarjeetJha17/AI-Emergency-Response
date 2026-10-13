targetScope = 'resourceGroup'

@description('Existing Azure AI account name in this resource group.')
param aiResourceName string

@description('System-assigned managed identity principal ID of the staging API.')
param apiPrincipalId string

resource aiAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: aiResourceName
}

resource apiAiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiAccount.id, apiPrincipalId, 'Cognitive Services OpenAI User')
  scope: aiAccount
  properties: {
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    )
  }
}

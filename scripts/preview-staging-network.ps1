param(
    [string] $ResourceGroupName = 'rg-er-staging-eastus',
    [string] $Location = 'eastus',
    [string] $DatabaseSkuName = 'GP_S_Gen5_1',
    [ValidateSet('Template', 'Provider', 'ProviderNoRbac')]
    [string] $ValidationLevel = 'Template'
)

$ErrorActionPreference = 'Stop'

$accountJson = & az account show --query '{name:name,id:id}' --output json
if ($LASTEXITCODE -ne 0) {
    throw 'Azure CLI has no usable active subscription. Run az login and select the intended subscription first.'
}
$account = $accountJson | ConvertFrom-Json

$principalJson = & az ad signed-in-user show --query '{displayName:displayName,id:id}' --output json
if ($LASTEXITCODE -ne 0) {
    throw 'Could not read the signed-in Microsoft Entra user. Supply an approved SQL administrator identity before previewing.'
}
$principal = $principalJson | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($principal.displayName) -or
    [string]::IsNullOrWhiteSpace($principal.id)) {
    throw 'The signed-in Entra user response did not include both a display name and object ID.'
}

Write-Host "Preview subscription: $($account.name)"
Write-Host "Preview resource group: $ResourceGroupName ($Location)"
Write-Host "Validation level: $ValidationLevel"
Write-Host 'Running subscription-scope what-if only; no Azure deployment will be submitted.'

$parameters = @(
    "stagingResourceGroupName=$ResourceGroupName",
    "location=$Location",
    "sqlEntraAdminLogin=$($principal.displayName)",
    "sqlEntraAdminObjectId=$($principal.id)",
    "sqlDatabaseSkuName=$DatabaseSkuName"
)

$whatIfOutput = & az deployment sub what-if `
    --name 'er-staging-network-preview' `
    --location $Location `
    --template-file 'infra/staging-preview.bicep' `
    --parameters $parameters `
    --validation-level $ValidationLevel `
    --output table
$whatIfExitCode = $LASTEXITCODE

if ($whatIfExitCode -ne 0) {
    throw 'Azure what-if failed. No deployment was submitted.'
}

if ([string]::IsNullOrWhiteSpace(($whatIfOutput -join ''))) {
    throw 'Azure CLI returned no what-if report. No deployment was submitted.'
}

Write-Host 'What-if report:'
Write-Host ($whatIfOutput -join [Environment]::NewLine)

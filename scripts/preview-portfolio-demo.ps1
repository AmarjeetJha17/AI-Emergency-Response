param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroupName,
    [Parameter(Mandatory = $true)]
    [string] $Location,
    [Parameter(Mandatory = $true)]
    [string] $AiResourceGroupName,
    [Parameter(Mandatory = $true)]
    [string] $AiResourceName,
    [string] $ExpectedSubscriptionName = 'Azure subscription 1',
    [string] $ImageTag = 'preflight-0000000',
    [bool] $GrantAiInferenceRole = $false
)

$ErrorActionPreference = 'Stop'

$accountJson = & az account show --query '{name:name,state:state}' --output json
if ($LASTEXITCODE -ne 0) {
    throw 'Azure CLI has no usable active subscription. Run az login and select the intended subscription first.'
}
$account = $accountJson | ConvertFrom-Json
if ($account.name -ne $ExpectedSubscriptionName -or $account.state -ne 'Enabled') {
    throw "Active subscription must be enabled '$ExpectedSubscriptionName'; found '$($account.name)' with state '$($account.state)'."
}

$principalJson = & az ad signed-in-user show --query '{displayName:displayName,id:id}' --output json
if ($LASTEXITCODE -ne 0) {
    throw 'Could not read the signed-in Microsoft Entra user for the SQL administrator.'
}
$principal = $principalJson | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($principal.displayName) -or
    [string]::IsNullOrWhiteSpace($principal.id)) {
    throw 'The signed-in Entra user response did not include a display name and object ID.'
}

Write-Host "Preview subscription: $($account.name)"
Write-Host "Portfolio resource group: $ResourceGroupName ($Location)"
Write-Host "Existing AI account: $AiResourceGroupName/$AiResourceName"
Write-Host "AI inference role assignment included: $GrantAiInferenceRole"
Write-Host 'Running subscription-scope what-if only; no deployment will be submitted.'

$parameters = @(
    "resourceGroupName=$ResourceGroupName",
    "location=$Location",
    "aiResourceGroupName=$AiResourceGroupName",
    "aiResourceName=$AiResourceName",
    "imageTag=$ImageTag",
    "sqlEntraAdminLogin=$($principal.displayName)",
    "sqlEntraAdminObjectId=$($principal.id)",
    'deployApi=false',
    'deployWeb=false',
    "grantAiInferenceRole=$($GrantAiInferenceRole.ToString().ToLowerInvariant())",
    'aiDeploymentAvailabilityVerified=false'
)

$whatIfOutput = & az deployment sub what-if `
    --name 'er-portfolio-demo-preflight' `
    --location $Location `
    --template-file 'infra/portfolio-demo.bicep' `
    --parameters $parameters `
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

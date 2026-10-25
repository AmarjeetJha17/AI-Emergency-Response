[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$subscriptionId = '0a05d5ff-7fef-4886-8e72-22a6cafd5567'
$resourceGroupName = 'rg-er-portfolio-demo-centralus'
$resourceGroupLocation = 'centralus'
$environmentName = 'cae-er-portfolio-demo'
$apiAppName = 'ca-er-api-portfolio'
$webAppName = 'ca-er-web-portfolio'
$webIdentityName = 'id-er-portfolio-web'
$registryName = 'acerportfoliobxwiyolb'
$registryResourceGroup = 'rg-er-portfolio-demo-eastus2'
$expectedWebDigest = 'sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b'
$expectedApiUrl = 'https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io'
$templatePath = Join-Path $PSScriptRoot '..\..\infra\portfolio-demo-web.bicep'
$compiledTemplatePath = Join-Path ([System.IO.Path]::GetTempPath()) ('portfolio-demo-web-{0}.json' -f [guid]::NewGuid().ToString('N'))
$previewOnlySyncfusionPlaceholder = 'PREVIEW_ONLY_DISPOSABLE_NOT_A_LICENSE_KEY'
$az = Get-Command az -ErrorAction Stop
$bicep = Get-Command bicep -ErrorAction SilentlyContinue
if (-not $bicep) {
    $candidateBicep = Join-Path $env:LOCALAPPDATA 'Programs\Bicep CLI\bicep.exe'
    if (Test-Path -LiteralPath $candidateBicep) {
        $bicepPath = $candidateBicep
    } else {
        throw 'Bicep CLI was not found. Install or configure Bicep CLI, then rerun the preview.'
    }
} else {
    $bicepPath = $bicep.Source
}
function Invoke-AzJson {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$Stage
    )

    # Capture both native streams so Azure CLI failures cannot leak raw diagnostics.
    # Successful calls use --only-show-errors and return JSON on stdout.
    $output = & $az.Source @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Azure CLI failed during $Stage (exit code $exitCode). Details were suppressed."
    }
    try {
        return ($output -join [Environment]::NewLine) | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "Azure CLI returned an unreadable response during $Stage. Details were suppressed."
    }
}

function Assert-Equal {
    param([string]$Actual, [string]$Expected, [string]$Description)
    if ($Actual -cne $Expected) {
        throw "$Description did not match the expected value. Preview stopped."
    }
}

function Assert-Location {
    param([string]$Actual, [string]$Expected, [string]$Description)
    # Azure CLI/resource providers may return either the ARM token (centralus)
    # or its display form (Central US). Compare after removing separators.
    $actualToken = ($Actual -replace '[^A-Za-z0-9]', '').ToLowerInvariant()
    $expectedToken = ($Expected -replace '[^A-Za-z0-9]', '').ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($actualToken) -or $actualToken -cne $expectedToken) {
        throw "$Description did not match the expected Azure region. Preview stopped."
    }
}

function Get-ResourceGroupResourceId {
    param([string]$ProviderType, [string]$Name)
    return "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/$ProviderType/$Name"
}

function Get-SafePreviewChanges {
    param([Parameter(Mandatory)]$WhatIfResult)

    $statusProperty = $WhatIfResult.PSObject.Properties['status']
    $propertiesProperty = $WhatIfResult.PSObject.Properties['properties']
    if (-not $statusProperty -or [string]$statusProperty.Value -cne 'Succeeded' -or -not $propertiesProperty) {
        throw 'ARM What-If did not complete successfully. No changes are accepted.'
    }
    $changesProperty = $propertiesProperty.Value.PSObject.Properties['changes']
    if (-not $changesProperty -or $null -eq $changesProperty.Value -or
        $changesProperty.Value -isnot [System.Collections.IEnumerable] -or
        $changesProperty.Value -is [string]) {
        throw 'ARM What-If did not return an inspectable changes collection.'
    }

    $rows = @()
    foreach ($change in $changesProperty.Value) {
        if ([string]::IsNullOrWhiteSpace([string]$change.changeType) -or
            [string]::IsNullOrWhiteSpace([string]$change.resourceId)) {
            throw 'ARM What-If returned a malformed change record.'
        }
        $rows += [pscustomobject]@{
            ChangeType = [string]$change.changeType
            ResourceId = [string]$change.resourceId
        }
    }
    if ($rows.Count -eq 0) {
        throw 'ARM What-If returned no resource changes to inspect.'
    }
    return $rows
}

function Get-ArmResponseHeaderValue {
    param(
        [Parameter(Mandatory)][object]$Response,
        [Parameter(Mandatory)][string]$Name
    )

    $headersProperty = $Response.PSObject.Properties['Headers']
    if (-not $headersProperty -or $null -eq $headersProperty.Value) { return $null }
    $headers = $headersProperty.Value
    $headerValue = $null
    if ($headers -is [System.Collections.IDictionary]) {
        foreach ($key in $headers.Keys) {
            if ([string]$key -ieq $Name) { $headerValue = $headers[$key]; break }
        }
    } else {
        $headerProperty = $headers.PSObject.Properties[$Name]
        if ($headerProperty) { $headerValue = $headerProperty.Value }
    }
    if ($headerValue -is [array]) {
        if ($headerValue.Count -gt 0) { return [string]$headerValue[0] }
        return $null
    }
    if ($null -ne $headerValue) { return [string]$headerValue }
    return $null
}

try {
    if (-not (Test-Path -LiteralPath $templatePath)) {
        throw 'The isolated Web Bicep template was not found.'
    }

    $account = Invoke-AzJson -Arguments @('account','show','--output','json','--only-show-errors') -Stage 'checking the active subscription'
    Assert-Equal -Actual ([string]$account.id) -Expected $subscriptionId -Description 'Active subscription'
    if ([string]$account.state -ne 'Enabled') { throw 'The expected Azure subscription is not enabled.' }

    $group = Invoke-AzJson -Arguments @('group','show','--name',$resourceGroupName,'--output','json','--only-show-errors') -Stage 'checking the target resource group'
    Assert-Equal -Actual ([string]$group.id) -Expected "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName" -Description 'Target resource group'
    Assert-Location -Actual ([string]$group.location) -Expected $resourceGroupLocation -Description 'Target resource group region'

    $api = Invoke-AzJson -Arguments @('containerapp','show','--name',$apiAppName,'--resource-group',$resourceGroupName,'--output','json','--only-show-errors') -Stage 'checking the existing API app'
    Assert-Location -Actual ([string]$api.location) -Expected $resourceGroupLocation -Description 'API app region'
    if ([string]$api.properties.provisioningState -ne 'Succeeded') { throw 'The existing API app is not provisioned successfully.' }
    Assert-Equal -Actual ([string]$api.properties.configuration.ingress.fqdn) -Expected ([uri]$expectedApiUrl).Host -Description 'Existing API hostname'

    $environment = Invoke-AzJson -Arguments @('containerapp','env','show','--name',$environmentName,'--resource-group',$resourceGroupName,'--output','json','--only-show-errors') -Stage 'checking the existing Container Apps environment'
    Assert-Location -Actual ([string]$environment.location) -Expected $resourceGroupLocation -Description 'Container Apps environment region'
    if ([string]$environment.properties.provisioningState -ne 'Succeeded') { throw 'The existing Container Apps environment is not provisioned successfully.' }

    $webApps = Invoke-AzJson -Arguments @('containerapp','list','--resource-group',$resourceGroupName,'--query',"[?name=='$webAppName']",'--output','json','--only-show-errors') -Stage 'checking whether the Web app already exists'
    if (@($webApps).Count -ne 0) { throw 'The target Web Container App already exists; this create-only preview is stopped.' }

    $identity = Invoke-AzJson -Arguments @('identity','show','--name',$webIdentityName,'--resource-group',$resourceGroupName,'--output','json','--only-show-errors') -Stage 'checking the existing Web identity'
    if ([string]::IsNullOrWhiteSpace([string]$identity.id) -or [string]::IsNullOrWhiteSpace([string]$identity.clientId) -or [string]::IsNullOrWhiteSpace([string]$identity.principalId)) {
        throw 'The existing Web identity response was incomplete.'
    }

    $registry = Invoke-AzJson -Arguments @('acr','show','--name',$registryName,'--resource-group',$registryResourceGroup,'--output','json','--only-show-errors') -Stage 'checking the East US 2 registry'
    Assert-Location -Actual ([string]$registry.location) -Expected 'eastus2' -Description 'Registry region'
    Assert-Equal -Actual ([string]$registry.loginServer).ToLowerInvariant() -Expected 'acerportfoliobxwiyolb.azurecr.io' -Description 'Registry login server'
    $registryId = [string]$registry.id
    if ([string]::IsNullOrWhiteSpace($registryId)) { throw 'The registry resource ID could not be verified.' }

    # Without --all, Azure CLI returns assignments at this exact scope; --all conflicts with --scope.
    $assignments = Invoke-AzJson -Arguments @('role','assignment','list','--scope',$registryId,'--output','json','--only-show-errors') -Stage 'checking registry pull permission'
    $hasAcrPull = @($assignments | Where-Object {
        ([string]$_.principalId -ieq [string]$identity.principalId) -and
        ([string]$_.roleDefinitionName -ceq 'AcrPull') -and
        ([string]$_.scope.TrimEnd('/') -ieq $registryId.TrimEnd('/'))
    }).Count -gt 0
    if (-not $hasAcrPull) {
        throw 'The Web identity does not have a verified AcrPull assignment directly scoped to the East US 2 registry. No role assignment was created.'
    }

    $manifest = Invoke-AzJson -Arguments @('acr','manifest','show-metadata','--registry',$registryName,'--name',"emergencyresponse-web@$expectedWebDigest",'--output','json','--only-show-errors') -Stage 'checking the immutable Web image digest'
    Assert-Equal -Actual ([string]$manifest.digest).ToLowerInvariant() -Expected $expectedWebDigest -Description 'Web image digest'

    $storageContainerId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Storage/storageAccounts/sterportfolioqqgqkekj/blobServices/default/containers/web-data-protection"
    $container = Invoke-AzJson -Arguments @('resource','show','--ids',$storageContainerId,'--api-version','2023-05-01','--output','json','--only-show-errors') -Stage 'checking the existing private Data Protection container'
    if ([string]$container.properties.publicAccess -notin @('', 'None', $null)) {
        throw 'The existing Data Protection container is not private; preview stopped.'
    }

    Write-Host 'Verified target subscription, Central US resource group, API app, and Container Apps environment.'
    Write-Host 'Verified Web managed identity and exact registry-scoped AcrPull assignment.'
    Write-Host ("Verified immutable Web image digest: {0}" -f $manifest.digest)
    Write-Warning 'This preview configures unwrapped Blob-persisted Data Protection keys. Use only synthetic PortfolioDemo data; these keys are not wrapped by Key Vault.'

    & $bicepPath build $templatePath --outfile $compiledTemplatePath 2>$null | Out-Null
    $bicepExitCode = $LASTEXITCODE
    if ($bicepExitCode -ne 0) { throw "Bicep compilation failed (exit code $bicepExitCode). Details were suppressed." }
    $compiled = Get-Content -LiteralPath $compiledTemplatePath -Raw | ConvertFrom-Json -ErrorAction Stop
    if (@($compiled.resources).Count -ne 1 -or
        [string]$compiled.resources[0].type -cne 'Microsoft.App/containerApps' -or
        [string]$compiled.resources[0].name -cne "[variables('appName')]" -or
        [string]$compiled.parameters.syncfusionLicenseKey.type -cne 'securestring') {
        throw 'Compiled template did not meet the isolated one-resource and secure-parameter requirements.'
    }

    $suffix = 'preview' + [DateTime]::UtcNow.ToString('yyMMddHHmmss')
    if (-not (Get-Command Invoke-AzRestMethod -ErrorAction SilentlyContinue)) {
        Import-Module Az.Accounts -ErrorAction Stop
    }
    $armContext = Get-AzContext -ErrorAction Stop
    if (-not $armContext -or [string]$armContext.Subscription.Id -cne $subscriptionId) {
        throw 'The active Az PowerShell context does not match the verified Azure subscription. Preview stopped.'
    }

    $templateObject = $compiled
    $requestBody = @{
        properties = @{
            mode = 'Incremental'
            template = $templateObject
            parameters = @{
                revisionSuffix = @{ value = $suffix }
                syncfusionLicenseKey = @{ value = $previewOnlySyncfusionPlaceholder }
            }
            whatIfSettings = @{ resultFormat = 'ResourceIdOnly' }
        }
    }
    $requestPayload = ConvertTo-Json -InputObject $requestBody -Depth 100 -Compress -ErrorAction Stop
    $deploymentName = 'portfolio-web-preview-' + [guid]::NewGuid().ToString('N').Substring(0,12)
    $whatIfUri = [uri]("https://management.azure.com/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/{2}/whatIf?api-version=2025-04-01" -f $subscriptionId, [uri]::EscapeDataString($resourceGroupName), [uri]::EscapeDataString($deploymentName))
    $response = $null
    try {
        $response = Invoke-AzRestMethod -Uri $whatIfUri -Method POST -Payload $requestPayload -ErrorAction Stop
    } catch {
        throw 'ARM What-If request failed. Details were suppressed; no changes are accepted.'
    }

    $statusCode = [int]$response.StatusCode
    if ($statusCode -eq 202) {
        $locationHeader = Get-ArmResponseHeaderValue -Response $response -Name 'Location'
        $retryAfterHeader = Get-ArmResponseHeaderValue -Response $response -Name 'Retry-After'
        if ([string]::IsNullOrWhiteSpace($locationHeader)) {
            throw 'ARM accepted What-If without a polling location. No changes are accepted.'
        }
        $pollUri = $null
        if (-not [uri]::TryCreate($locationHeader, [System.UriKind]::Absolute, [ref]$pollUri)) {
            $pollUri = [uri]::new($whatIfUri, $locationHeader)
        }
        if ($pollUri.Scheme -ne 'https' -or $pollUri.Host -ine $whatIfUri.Host -or $pollUri.Port -ne $whatIfUri.Port) {
            throw 'ARM What-If returned an unexpected polling location. No changes are accepted.'
        }
        $pollAttempt = 0
        while ($statusCode -eq 202 -and $pollAttempt -lt 180) {
            $delaySeconds = 5
            $retryAfterSeconds = 0
            if ([int]::TryParse([string]$retryAfterHeader, [ref]$retryAfterSeconds) -and $retryAfterSeconds -gt 0) {
                $delaySeconds = [Math]::Min($retryAfterSeconds, 30)
            }
            Start-Sleep -Seconds $delaySeconds
            try {
                $response = Invoke-AzRestMethod -Uri $pollUri -Method GET -ErrorAction Stop
            } catch {
                throw 'ARM What-If polling failed. Details were suppressed; no changes are accepted.'
            }
            $statusCode = [int]$response.StatusCode
            $pollAttempt++
            if ($statusCode -eq 202) {
                $nextRetryAfter = Get-ArmResponseHeaderValue -Response $response -Name 'Retry-After'
                if ($nextRetryAfter) { $retryAfterHeader = $nextRetryAfter }
            }
        }
        if ($statusCode -eq 202) { throw 'ARM What-If exceeded the bounded polling period. No changes are accepted.' }
    }
    if ($statusCode -ne 200) { throw "ARM What-If returned HTTP $statusCode. No changes are accepted." }
    try {
        $whatIf = ConvertFrom-Json -InputObject ([string]$response.Content) -ErrorAction Stop
    } catch {
        throw 'ARM What-If returned malformed JSON. No changes are accepted.'
    }
    $requestPayload = $null
    $requestBody = $null
    $templateObject = $null
    $rows = @(Get-SafePreviewChanges -WhatIfResult $whatIf)
    Write-Host 'Sanitized What-If resource changes:'
    foreach ($row in $rows) { Write-Host ("  {0}  {1}" -f $row.ChangeType, $row.ResourceId) }

    $actionable = @($rows | Where-Object { $_.ChangeType -notin @('Ignore','NoChange') })
    $webResourceId = Get-ResourceGroupResourceId -ProviderType 'Microsoft.App/containerApps' -Name $webAppName
    if ($actionable.Count -ne 1 -or $actionable[0].ChangeType -cne 'Create' -or $actionable[0].ResourceId.TrimEnd('/') -ine $webResourceId.TrimEnd('/')) {
        throw 'What-If did not predict exactly one Create for the Web Container App and no other actionable changes. Preview gate failed.'
    }
    Write-Host 'PREVIEW GATE PASSED: the only actionable change is Create of ca-er-web-portfolio.'
    Write-Host 'This script is preview-only. No deployment was submitted.'
} finally {
    $whatIfOutput = $null
    $requestPayload = $null
    $requestBody = $null
    $templateObject = $null
    $response = $null
    $previewOnlySyncfusionPlaceholder = $null
    if (Test-Path -LiteralPath $compiledTemplatePath) {
        Remove-Item -LiteralPath $compiledTemplatePath -Force -ErrorAction SilentlyContinue
    }
}

\n
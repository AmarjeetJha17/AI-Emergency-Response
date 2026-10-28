[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# This script can create only the isolated Web Container App declared in
# infra/portfolio-demo-web.bicep. It never changes the API or shared resources.
$subscriptionId = '0a05d5ff-7fef-4886-8e72-22a6cafd5567'
$resourceGroup = 'rg-er-portfolio-demo-centralus'
$webAppName = 'ca-er-web-portfolio'
$expectedApiFqdn = 'ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io'
$expectedImageDigest = 'sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b'
$registryName = 'acerportfoliobxwiyolb'
$repository = 'emergencyresponse-web'
$imageTag = 'portfolio-20261024-210419-dp10passed'
$expectedImage = "$registryName.azurecr.io/$repository@$expectedImageDigest"

$templatePath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\..\infra\portfolio-demo-web.bicep'))
$previewScriptPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot 'preview-portfolio-demo-web.ps1'))
$tempCompiledTemplate = Join-Path ([System.IO.Path]::GetTempPath()) (
    'portfolio-demo-web-deploy-{0}.json' -f [guid]::NewGuid().ToString('N'))

$licenseSecure = $null
$licensePlain = $null
$whatIfPayloadJson = $null
$deploymentPayloadJson = $null
$whatIfResponseContent = $null
$deploymentResponseContent = $null
$deploymentStarted = $false

function Invoke-AzCli {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation,
        [switch] $AsJson
    )

    $safeArguments = @($Arguments) + @('--only-show-errors')
    $nativeOutput = & az @safeArguments 2>&1
    $nativeExitCode = $LASTEXITCODE
    $outputText = (@($nativeOutput) | ForEach-Object { [string]$_ }) -join [Environment]::NewLine

    if ($nativeExitCode -ne 0) {
        # None of the calls to this helper accepts secret material.
        throw "Azure CLI operation '$Operation' failed (exit $nativeExitCode). $outputText"
    }

    if (-not $AsJson) {
        return $outputText.Trim()
    }

    try {
        return $outputText | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Azure CLI operation '$Operation' returned invalid JSON."
    }
}

function Convert-SecureStringToPlainTextInMemory {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [Security.SecureString] $Value)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Invoke-ArmRestAndGetJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [ValidateSet('POST', 'PUT')] [string] $Method,
        [Parameter(Mandatory)] [string] $Payload,
        [Parameter(Mandatory)] [string] $Operation
    )

    try {
        $response = Invoke-AzRestMethod -Method $Method -Path $Path -Payload $Payload -WaitForCompletion -ErrorAction Stop
        $content = [string]$response.Content
        if ([string]::IsNullOrWhiteSpace($content)) {
            throw "ARM operation '$Operation' returned an empty response body."
        }

        return ($content | ConvertFrom-Json -ErrorAction Stop)
    }
    catch {
        # Do not echo the HTTP body or request payload. Both can include the
        # real Syncfusion license key while it is held in memory for ARM calls.
        $message = [string]$_.Exception.Message
        if (-not [string]::IsNullOrEmpty($script:licensePlain)) {
            $message = $message.Replace($script:licensePlain, '[REDACTED]')
        }
        throw "ARM operation '$Operation' failed. Secret-bearing request/response content was suppressed. Detail: $message"
    }
}

function Get-ArmResponseHeaderValue {
    param(
        [Parameter(Mandatory)] [object] $Response,
        [Parameter(Mandatory)] [string] $Name
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

function Invoke-ArmWhatIfAndGetJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Payload,
        [Parameter(Mandatory)] [string] $Operation
    )

    $uri = [uri]("https://management.azure.com{0}" -f $Path)
    $response = $null
    try {
        # Invoke What-If directly; manually poll 202 so cmdlet wait wrappers cannot
        # hide the structured ARM result or expose request/response details.
        $response = Invoke-AzRestMethod -Uri $uri -Method POST -Payload $Payload -ErrorAction Stop
    } catch {
        throw "ARM operation '$Operation' failed while submitting the What-If request. Response details were suppressed."
    }

    $statusCode = [int]$response.StatusCode
    if ($statusCode -eq 202) {
        $pollLocation = Get-ArmResponseHeaderValue -Response $response -Name 'Location'
        if ([string]::IsNullOrWhiteSpace($pollLocation)) {
            $pollLocation = Get-ArmResponseHeaderValue -Response $response -Name 'Azure-AsyncOperation'
        }
        $retryAfter = Get-ArmResponseHeaderValue -Response $response -Name 'Retry-After'
        if ([string]::IsNullOrWhiteSpace($pollLocation)) {
            throw "ARM operation '$Operation' returned HTTP 202 without a polling location. No changes are accepted."
        }

        $pollUri = $null
        if (-not [uri]::TryCreate($pollLocation, [System.UriKind]::Absolute, [ref]$pollUri)) {
            $pollUri = [uri]::new($uri, $pollLocation)
        }
        if ($pollUri.Scheme -ne 'https' -or $pollUri.Host -ine $uri.Host -or $pollUri.Port -ne $uri.Port) {
            throw "ARM operation '$Operation' returned an unexpected polling location. No changes are accepted."
        }

        $pollAttempt = 0
        while ($statusCode -eq 202 -and $pollAttempt -lt 180) {
            $delaySeconds = 5
            $retryAfterSeconds = 0
            if ([int]::TryParse([string]$retryAfter, [ref]$retryAfterSeconds) -and $retryAfterSeconds -gt 0) {
                $delaySeconds = [Math]::Min($retryAfterSeconds, 30)
            }
            Start-Sleep -Seconds $delaySeconds
            try {
                $response = Invoke-AzRestMethod -Uri $pollUri -Method GET -ErrorAction Stop
            } catch {
                throw "ARM operation '$Operation' failed while polling. Response details were suppressed."
            }
            $statusCode = [int]$response.StatusCode
            $pollAttempt++
            if ($statusCode -eq 202) {
                $nextRetryAfter = Get-ArmResponseHeaderValue -Response $response -Name 'Retry-After'
                if ($nextRetryAfter) { $retryAfter = $nextRetryAfter }
            }
        }
        if ($statusCode -eq 202) {
            throw "ARM operation '$Operation' exceeded the bounded polling period. No changes are accepted."
        }
    }

    if ($statusCode -ne 200) {
        throw "ARM operation '$Operation' returned HTTP $statusCode. Response details were suppressed."
    }
    $content = [string]$response.Content
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "ARM operation '$Operation' returned an empty result. No changes are accepted."
    }
    try {
        return ConvertFrom-Json -InputObject $content -ErrorAction Stop
    } catch {
        throw "ARM operation '$Operation' returned malformed JSON. No changes are accepted."
    }
}

try {
    if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
        throw "Web Bicep template not found: $templatePath"
    }
    if (-not (Test-Path -LiteralPath $previewScriptPath -PathType Leaf)) {
        throw "Required preview gate not found: $previewScriptPath"
    }

    if (-not (Get-Command Invoke-AzRestMethod -ErrorAction SilentlyContinue)) {
        throw 'Az.Accounts with Invoke-AzRestMethod is required. Install/import Az.Accounts, sign in, and retry.'
    }

    $context = Get-AzContext -ErrorAction SilentlyContinue
    if ($null -eq $context -or [string]$context.Subscription.Id -ne $subscriptionId) {
        if ($null -eq $context) {
            Connect-AzAccount -Tenant '6dd6ceca-7611-4f70-a770-7f74506eeda7' -ErrorAction Stop | Out-Null
        }
        Set-AzContext -SubscriptionId $subscriptionId -ErrorAction Stop | Out-Null
    }
    $context = Get-AzContext -ErrorAction Stop
    if ([string]$context.Subscription.Id -ne $subscriptionId) {
        throw 'Azure PowerShell context is not set to the expected subscription.'
    }

    Write-Host 'Re-running the full read-only preview gate (Azure CLI checks and What-If)...'
    & $previewScriptPath

    # Capture a fresh local Bicep build for the actual What-If and deployment.
    # Build output is captured so no incidental output can be mistaken for success.
    $buildOutput = & az bicep build --file $templatePath --outfile $tempCompiledTemplate --only-show-errors 2>&1
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -ne 0 -or -not (Test-Path -LiteralPath $tempCompiledTemplate -PathType Leaf)) {
        throw 'Bicep compilation failed. No deployment was submitted.'
    }

    $compiledTemplate = Get-Content -LiteralPath $tempCompiledTemplate -Raw | ConvertFrom-Json -ErrorAction Stop
    $resources = @($compiledTemplate.resources)
    $compiledResourceName = if ($resources.Count -eq 1) { [string]$resources[0].name } else { '' }
    $resourceNameMatches = $compiledResourceName -ceq $webAppName
    if (-not $resourceNameMatches -and
        $compiledResourceName -ceq "[variables('appName')]" -and
        [string]$compiledTemplate.variables.appName -ceq $webAppName) {
        $resourceNameMatches = $true
    }
    if ($resources.Count -ne 1 -or
        [string]$resources[0].type -ne 'Microsoft.App/containerApps' -or
        -not $resourceNameMatches -or
        [string]$compiledTemplate.parameters.syncfusionLicenseKey.type -cne 'securestring') {
        throw 'Fail-closed: compiled template does not contain exactly one Web Container App resource.'
    }

    # Ask for the real license key only after all no-secret preflight checks pass.
    Write-Host ''
    Write-Host 'Enter the real Syncfusion license key at the hidden prompt. Do not paste it into chat or a command.'
    $licenseSecure = Read-Host -Prompt 'Syncfusion license key (input hidden)' -AsSecureString
    if ($null -eq $licenseSecure -or $licenseSecure.Length -eq 0) {
        throw 'An empty Syncfusion license key was supplied. No deployment was submitted.'
    }
    $licensePlain = Convert-SecureStringToPlainTextInMemory -Value $licenseSecure
    if ([string]::IsNullOrWhiteSpace($licensePlain)) {
        throw 'Syncfusion license key is empty or whitespace. No deployment was submitted.'
    }

    # The real value remains in process memory only. It is sent to ARM over the
    # authenticated HTTPS session; it is not placed in CLI arguments, files,
    # console output, or a parameter file.
    $revisionSuffix = 'web' + [guid]::NewGuid().ToString('N').Substring(0, 12)
    $whatIfBody = @{
        properties = @{
            mode = 'Incremental'
            template = $compiledTemplate
            parameters = @{
                revisionSuffix = @{ value = $revisionSuffix }
                syncfusionLicenseKey = @{ value = $licensePlain }
            }
            whatIfSettings = @{ resultFormat = 'FullResourcePayloads' }
        }
    }
    $whatIfPayloadJson = ConvertTo-Json -InputObject $whatIfBody -Depth 100 -Compress
    $deploymentName = 'portfolio-web-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    $whatIfPath = "/subscriptions/$subscriptionId/resourcegroups/$resourceGroup/providers/Microsoft.Resources/deployments/$deploymentName-whatif/whatIf?api-version=2025-04-01"

    Write-Host 'Running a fresh What-If with the actual secure parameter. Resource payloads will not be printed.'
    $whatIfResponse = Invoke-ArmWhatIfAndGetJson -Path $whatIfPath -Payload $whatIfPayloadJson -Operation 'Web deployment What-If'
    $whatIfResponseContent = $null
    if (-not $whatIfResponse -or [string]$whatIfResponse.status -cne 'Succeeded' -or
        $null -eq $whatIfResponse.properties.changes) {
        throw 'Fail-closed: ARM What-If did not return a completed, inspectable changes collection. No deployment was submitted.'
    }
    $changes = @($whatIfResponse.properties.changes)
    $actionableChanges = @($changes | Where-Object { [string]$_.changeType -notin @('Ignore', 'NoChange') })
    if ($actionableChanges.Count -ne 1) {
        throw "Fail-closed: expected exactly one actionable change; What-If returned $($actionableChanges.Count). No deployment was submitted."
    }

    $change = $actionableChanges[0]
    $expectedResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.App/containerApps/$webAppName"
    $actualResourceId = ([string]$change.resourceId).TrimEnd('/')
    if ([string]$change.changeType -ne 'Create' -or
        $actualResourceId -ine $expectedResourceId) {
        throw "Fail-closed: What-If expected Create of '$webAppName' only; actual change type/resource was not accepted. No deployment was submitted."
    }

    Write-Host ''
    Write-Host '=== FINAL DEPLOYMENT REVIEW ===' -ForegroundColor Yellow
    Write-Host "Subscription: $subscriptionId"
    Write-Host "Resource group: $resourceGroup"
    Write-Host "Resource change: Create $webAppName"
    Write-Host "Pinned image: $expectedImage"
    Write-Host 'Data Protection: Blob-persisted unwrapped keys, synthetic PortfolioDemo only'
    Write-Host 'The existing API and shared infrastructure are not in this template.'
    Write-Host 'The actual license key will not be displayed.'
    Write-Host ''
    $confirmation = Read-Host "Type DEPLOY to submit only this Web Container App deployment"
    if ($confirmation -cne 'DEPLOY') {
        Write-Host 'Deployment cancelled. No resource deployment was submitted.' -ForegroundColor Yellow
        return
    }

    $deploymentBody = @{
        properties = @{
            mode = 'Incremental'
            template = $compiledTemplate
            parameters = @{
                revisionSuffix = @{ value = $revisionSuffix }
                syncfusionLicenseKey = @{ value = $licensePlain }
            }
        }
    }
    $deploymentPayloadJson = ConvertTo-Json -InputObject $deploymentBody -Depth 100 -Compress
    $deploymentPath = "/subscriptions/$subscriptionId/resourcegroups/$resourceGroup/providers/Microsoft.Resources/deployments/${deploymentName}?api-version=2022-09-01"

    Write-Host 'Submitting the reviewed Web-only deployment to Azure Resource Manager...'
    $deploymentStarted = $true
    $deploymentResponse = Invoke-ArmRestAndGetJson -Path $deploymentPath -Method PUT -Payload $deploymentPayloadJson -Operation 'Create Web Container App deployment'
    $deploymentResponseContent = $null

    $deploymentState = [string]$deploymentResponse.properties.provisioningState
    if ($deploymentState -ne 'Succeeded') {
        throw "ARM deployment returned state '$deploymentState'. Check deployment history in resource group '$resourceGroup'; no secret value was printed."
    }

    # Verify app existence/state and public FQDN without printing environment values or secrets.
    $webApp = Invoke-AzCli -Operation 'verify created Web Container App' -AsJson -Arguments @(
        'containerapp', 'show', '--subscription', $subscriptionId,
        '--resource-group', $resourceGroup, '--name', $webAppName,
        '--query', '{Name:name,ProvisioningState:properties.provisioningState,LatestRevision:properties.latestRevisionName,Fqdn:properties.configuration.ingress.fqdn,Image:properties.template.containers[0].image}',
        '--output', 'json'
    )
    if ([string]$webApp.Name -ne $webAppName -or
        [string]$webApp.ProvisioningState -ne 'Succeeded' -or
        [string]$webApp.Image -ne $expectedImage -or
        [string]::IsNullOrWhiteSpace([string]$webApp.Fqdn)) {
        throw 'Deployment call succeeded, but post-deployment app verification did not match the expected Web app/image. Inspect Container App revision and deployment state.'
    }

    Write-Host ''
    Write-Host '=== Web Container App created and verified ===' -ForegroundColor Green
    Write-Host "Deployment: $deploymentName"
    Write-Host "Provisioning state: $deploymentState"
    Write-Host "Web app: $($webApp.Name)"
    Write-Host "Latest revision: $($webApp.LatestRevision)"
    Write-Host "FQDN: $($webApp.Fqdn)"
    Write-Host "Image: $($webApp.Image)"
    Write-Host 'Next: verify revision health, /health/live, /health/ready, API calls, and Blob-backed Data Protection.'
}
catch {
    # Sanitize the current license wherever it could be echoed by an exception.
    $errorMessage = [string]$_.Exception.Message
    if (-not [string]::IsNullOrEmpty($licensePlain)) {
        $errorMessage = $errorMessage.Replace($licensePlain, '[REDACTED]')
    }
    Write-Error -Message $errorMessage -ErrorAction Continue
    throw 'Web deployment workflow stopped. Review the sanitized error above and Azure deployment history if submission had already started.'
}
finally {
    # Remove in-memory references and the compiled temporary template.
    $licensePlain = $null
    $whatIfPayloadJson = $null
    $deploymentPayloadJson = $null
    $whatIfResponseContent = $null
    $deploymentResponseContent = $null
    $compiledTemplate = $null
    $whatIfBody = $null
    $deploymentBody = $null
    if ($null -ne $licenseSecure) {
        $licenseSecure.Dispose()
        $licenseSecure = $null
    }
    if (Test-Path -LiteralPath $tempCompiledTemplate) {
        Remove-Item -LiteralPath $tempCompiledTemplate -Force -ErrorAction SilentlyContinue
    }
}

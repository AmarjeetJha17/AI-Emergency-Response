[CmdletBinding()]
param(
    [switch]$PreflightOnly
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'SilentlyContinue'
$DebugPreference = 'SilentlyContinue'
$ProgressPreference = 'SilentlyContinue'

$resourceGroupName = 'rg-er-portfolio-demo-centralus'
$apiAppName = 'ca-er-api-portfolio'
$registryResourceGroupName = 'rg-er-portfolio-demo-eastus2'
$registryName = 'acerportfoliobxwiyolb'
$registryLoginServer = 'acerportfoliobxwiyolb.azurecr.io'
$repositoryName = 'emergencyresponse-api'
$imageTag = 'portfolio-20261024-200121-sqlclient711'
$expectedImageDigest = 'sha256:c09b299562f3eaae4786b2361f73e9a214c157f3d6d863b6ca1858e070a8cb4a'
$expectedImage = "${registryLoginServer}/${repositoryName}:${imageTag}"
$adminPasswordSecure = $null
$jwtSigningKeySecure = $null
$parameterObject = $null
$compiledTemplate = $null
$compiledTemplateText = $null

function New-RandomSecureString {
    param(
        [Parameter(Mandatory)][int]$ByteCount,
        [string]$Suffix = ''
    )

    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] $ByteCount
    $plainValue = $null
    try {
        $generator.GetBytes($bytes)
        $plainValue = [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') + $Suffix
        return ConvertTo-SecureString -String $plainValue -AsPlainText -Force
    } finally {
        [Array]::Clear($bytes, 0, $bytes.Length)
        $generator.Dispose()
        $plainValue = $null
    }
}

function ConvertFrom-SecureStringForArmPayload {
    param([Parameter(Mandatory)][System.Security.SecureString]$SecureValue)

    $securePointer = [IntPtr]::Zero
    try {
        $securePointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($securePointer)
    } finally {
        if ($securePointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($securePointer)
        }
    }
}

function ConvertTo-TemplateHashtable {
    param([AllowNull()][object]$InputObject)

    if ($null -eq $InputObject) { return $null }
    if ($InputObject -is [System.Collections.IDictionary]) {
        $convertedMap = @{}
        foreach ($key in $InputObject.Keys) {
            $convertedMap[[string]$key] = ConvertTo-TemplateHashtable -InputObject $InputObject[$key]
        }
        return $convertedMap
    }
    if ($InputObject -is [System.Array]) {
        $convertedArray = @(
            foreach ($entry in $InputObject) { ConvertTo-TemplateHashtable -InputObject $entry }
        )
        return ,$convertedArray
    }
    if ($InputObject -is [System.Management.Automation.PSCustomObject]) {
        $convertedMap = @{}
        foreach ($property in $InputObject.PSObject.Properties) {
            $convertedMap[$property.Name] = ConvertTo-TemplateHashtable -InputObject $property.Value
        }
        return $convertedMap
    }
    return $InputObject
}

function Get-WhatIfPropertyDeltaSummaries {
    param([Parameter(Mandatory)][object]$Delta)

    $summaries = [System.Collections.Generic.List[object]]::new()
    $pending = [System.Collections.Generic.Stack[object]]::new()
    foreach ($entry in @($Delta)) {
        $pending.Push([pscustomobject]@{ Node = $entry; ParentPath = '' })
    }
    $visited = 0
    while ($pending.Count -gt 0) {
        $frame = $pending.Pop()
        $node = $frame.Node
        $visited++
        if ($visited -gt 2000 -or -not ($node -is [System.Collections.IDictionary])) {
            throw 'Azure What-If returned malformed or excessive property deltas. No deployment was submitted.'
        }

        $relativePath = [string]$node.path
        $propertyChangeType = [string]$node.propertyChangeType
        if ($relativePath -notmatch '^[A-Za-z0-9_./\[\]''-]{1,300}$' -or
            $propertyChangeType -notin @('Create', 'Delete', 'Modify', 'Array', 'NoEffect')) {
            throw 'Azure What-If returned an incomplete or unsafe property delta. No deployment was submitted.'
        }
        if (-not $frame.ParentPath) {
            $path = $relativePath
        } elseif ($relativePath -match '^(\d+)(?:\.(.*))?$') {
            $path = '{0}[{1}]' -f $frame.ParentPath, $Matches[1]
            if ($Matches[2]) { $path += '.' + $Matches[2] }
        } elseif ($relativePath.StartsWith('[')) {
            $path = $frame.ParentPath + $relativePath
        } else {
            $path = $frame.ParentPath + '.' + $relativePath
        }
        if ($path -notmatch '^[A-Za-z0-9_./\[\]''-]{1,500}$') {
            throw 'Azure What-If returned an unsafe composed property path. No deployment was submitted.'
        }

        $children = @()
        if ($node.Contains('children')) {
            if ($null -eq $node.children -or -not ($node.children -is [System.Collections.IEnumerable]) -or
                $node.children -is [string] -or $node.children -is [System.Collections.IDictionary]) {
                throw 'Azure What-If returned malformed nested property deltas. No deployment was submitted.'
            }
            $children = @($node.children)
        }
        $summaries.Add([pscustomobject]@{
            Path               = $path
            PropertyChangeType = $propertyChangeType
            HasChildren        = ($children.Count -gt 0)
        })

        foreach ($child in $children) {
            $pending.Push([pscustomobject]@{ Node = $child; ParentPath = $path })
        }
    }
    return $summaries.ToArray()
}

function Test-ExpectedApiWhatIfPropertyPath {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$PropertyChangeType,
        [Parameter(Mandatory)][bool]$HasChildren
    )

    # These are the only API app properties this update is intended to change:
    # the managed identity used for registry pulls, registry/native-secret
    # configuration, the image/revision, and the explicitly reviewed env list.
    # Array containers are accepted only as parents of inspectable child deltas.
    # Container Apps GET responses expose properties.runningStatus as runtime
    # state, while the 2024-03-01 Create or Update request contract omits it.
    # Allow only this exact response-only field's Delete delta; it is not a
    # requested change to runtime state. No other deletion is exempted.
    if ($Path -ceq 'properties.runningStatus' -and
        $PropertyChangeType -ceq 'Delete' -and
        -not $HasChildren) { return $true }

    # Apart from the response-only runtime field above, permit Delete only
    # when removing Key Vault metadata as part of the reviewed native-secret
    # conversion. A deletion of any other reviewed property is still rejected.
    if ($PropertyChangeType -ceq 'Delete' -and
        $Path -notmatch '^properties\.configuration\.secrets\[\d+\]\.(identity|keyVaultUrl)$') {
        return $false
    }

    if ($HasChildren -and $PropertyChangeType -eq 'Array' -and $Path -in @(
        'properties.template.containers',
        'properties.template.containers[0].env',
        'properties.configuration.secrets',
        'properties.configuration.registries'
    )) { return $true }
    if ($HasChildren -and $PropertyChangeType -eq 'Modify' -and $Path -match '^(properties\.template\.containers\[0\]|properties\.configuration\.(secrets|registries)\[\d+\])$') {
        return $true
    }

    $allowedPatterns = @(
        '^identity\.userAssignedIdentities(\[[^\]]+\])?(\.[A-Za-z0-9_-]+)?$',
        '^properties\.configuration\.registries\[\d+\]\.(server|identity)$',
        '^properties\.configuration\.secrets\[\d+\](\.(name|value|identity|keyVaultUrl|secretRef))?$',
        '^properties\.template\.revisionSuffix$',
        '^properties\.template\.containers\[\d+\]\.image$',
        '^properties\.template\.containers\[\d+\]\.env(\[\d+\])?(\.(name|value|secretRef))?$'
    )
    foreach ($pattern in $allowedPatterns) {
        if ($Path -match $pattern) { return $true }
    }
    return $false
}

function Test-AcrRepositoryTag {
    param(
        [Parameter(Mandatory)][string]$LoginServer,
        [Parameter(Mandatory)][string]$Repository,
        [Parameter(Mandatory)][string]$Tag,
        [Parameter(Mandatory)][System.Security.SecureString]$AccessToken
    )

    $tokenPointer = [IntPtr]::Zero
    $tokenPlaintext = $null
    $headers = $null
    try {
        $tokenPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AccessToken)
        $tokenPlaintext = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($tokenPointer)
        $headers = @{ Authorization = "Bearer $tokenPlaintext"; Accept = 'application/json' }
        $uri = 'https://{0}/acr/v1/{1}/_tags/{2}?api-version=2021-07-01' -f `
            $LoginServer, [uri]::EscapeDataString($Repository), [uri]::EscapeDataString($Tag)
        $metadata = Invoke-RestMethod -Uri $uri -Method GET -Headers $headers -ErrorAction Stop
        $manifestDigest = [string]$metadata.tag.digest
        if ([string]$metadata.imageName -cne $Repository -or [string]$metadata.tag.name -cne $Tag -or
            $manifestDigest -notmatch '^sha256:[0-9a-f]{64}$') {
            return [pscustomobject]@{ Succeeded = $false; Exists = $false; Digest = $manifestDigest; HttpStatus = 200; Category = 'uninspectable metadata response'; ExceptionType = $null }
        }
        return [pscustomobject]@{ Succeeded = $true; Exists = $true; Digest = $manifestDigest; HttpStatus = 200; Category = 'success'; ExceptionType = $null }
    } catch {
        $statusCode = 0
        $safeExceptionType = $null
        $exceptionTypeName = [string]$_.Exception.GetType().FullName
        if ($exceptionTypeName -match '^[A-Za-z_][A-Za-z0-9_.`+]{0,199}$') {
            $safeExceptionType = $exceptionTypeName
        }
        $responseProperty = $_.Exception.PSObject.Properties['Response']
        if ($responseProperty -and $responseProperty.Value) {
            $statusProperty = $responseProperty.Value.PSObject.Properties['StatusCode']
            if ($statusProperty -and $null -ne $statusProperty.Value) {
                if ($statusProperty.Value -is [System.Enum]) {
                    $statusCode = [int]$statusProperty.Value
                } else {
                    [void][int]::TryParse([string]$statusProperty.Value, [ref]$statusCode)
                }
            }
        }
        if ($statusCode -eq 404) {
            return [pscustomobject]@{ Succeeded = $true; Exists = $false; Digest = $null; HttpStatus = 404; Category = 'not found'; ExceptionType = $safeExceptionType }
        }
        $category = switch ($statusCode) {
            { $_ -in 401, 403 } { 'authorization failure'; break }
            429 { 'throttling'; break }
            { $_ -in 408, 500, 502, 503, 504 } { 'network/connectivity failure'; break }
            { $_ -ge 400 -and $_ -le 499 } { 'request failure'; break }
            default {
                if ($safeExceptionType -match '^System\.Net\.') { 'network/connectivity failure' }
                else { 'unclassified failure' }
            }
        }
        return [pscustomobject]@{ Succeeded = $false; Exists = $false; Digest = $null; HttpStatus = $statusCode; Category = $category; ExceptionType = $safeExceptionType }
    } finally {
        if ($headers) { $headers.Clear() }
        $tokenPlaintext = $null
        if ($tokenPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($tokenPointer)
        }
    }
}

function Get-AcrRepositoryAccessToken {
    param(
        [Parameter(Mandatory)][string]$LoginServer,
        [Parameter(Mandatory)][string]$Repository,
        [Parameter(Mandatory)][string]$TenantId,
        [Parameter(Mandatory)][System.Security.SecureString]$AzureAccessToken
    )

    $tokenPointer = [IntPtr]::Zero
    $azureTokenPlaintext = $null
    $acrRefreshToken = $null
    $acrAccessToken = $null
    $exchangeBody = $null
    $accessBody = $null
    $exchange = $null
    $tokenResponse = $null
    try {
        $tokenPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AzureAccessToken)
        $azureTokenPlaintext = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($tokenPointer)
        $exchangeBody = 'grant_type=access_token&service={0}&tenant={1}&access_token={2}' -f `
            [uri]::EscapeDataString($LoginServer), [uri]::EscapeDataString($TenantId), [uri]::EscapeDataString($azureTokenPlaintext)
        $exchange = Invoke-RestMethod `
            -Uri "https://$LoginServer/oauth2/exchange" `
            -Method POST `
            -ContentType 'application/x-www-form-urlencoded' `
            -Body $exchangeBody `
            -ErrorAction Stop
        $acrRefreshToken = [string]$exchange.refresh_token
        if ([string]::IsNullOrWhiteSpace($acrRefreshToken)) {
            throw 'ACR did not return an exchange token.'
        }

        $accessBody = 'grant_type=refresh_token&service={0}&scope={1}&refresh_token={2}' -f `
            [uri]::EscapeDataString($LoginServer), [uri]::EscapeDataString("repository:${Repository}:pull"), [uri]::EscapeDataString($acrRefreshToken)
        $tokenResponse = Invoke-RestMethod `
            -Uri "https://$LoginServer/oauth2/token" `
            -Method POST `
            -ContentType 'application/x-www-form-urlencoded' `
            -Body $accessBody `
            -ErrorAction Stop
        $acrAccessToken = [string]$tokenResponse.access_token
        if ([string]::IsNullOrWhiteSpace($acrAccessToken)) {
            throw 'ACR did not return a repository access token.'
        }
        return ConvertTo-SecureString -String $acrAccessToken -AsPlainText -Force
    } finally {
        $azureTokenPlaintext = $null
        $acrRefreshToken = $null
        $acrAccessToken = $null
        $exchangeBody = $null
        $accessBody = $null
        $exchange = $null
        $tokenResponse = $null
        if ($tokenPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($tokenPointer)
        }
    }
}

function Get-SafeWhatIfFailureDiagnostic {
    param([Parameter(Mandatory)][System.Management.Automation.ErrorRecord]$ErrorRecord)

    $codeCategories = @{
        AuthorizationFailed                   = 'authorization failure'
        Forbidden                             = 'authorization failure'
        Unauthorized                          = 'authorization failure'
        InvalidAuthenticationToken            = 'authorization failure'
        InvalidTemplate                       = 'template validation failure'
        InvalidTemplateDeployment             = 'template validation failure'
        DeploymentTemplateValidationFailed    = 'template validation failure'
        InvalidParameter                      = 'template validation failure'
        DeploymentFailed                      = 'deployment failure'
        ResourceNotFound                      = 'resource not found'
        ResourceGroupNotFound                 = 'resource not found'
        TooManyRequests                       = 'throttling'
        Throttled                             = 'throttling'
        RateLimitExceeded                     = 'throttling'
        RequestTimeout                        = 'network/connectivity failure'
        GatewayTimeout                        = 'network/connectivity failure'
        ServiceUnavailable                    = 'network/connectivity failure'
        NameResolutionFailure                 = 'network/connectivity failure'
        ConnectFailure                        = 'network/connectivity failure'
    }

    $recognizedCode = $null
    $category = 'unclassified failure'
    $safeExceptionType = $null
    $safePowerShellCategory = $null
    $safeHttpStatusCode = $null
    $safeAzErrorKind = $null
    $hasPowerShellException = $false
    $powerShellCategory = [string]$ErrorRecord.CategoryInfo.Category
    if ($powerShellCategory -in [Enum]::GetNames([System.Management.Automation.ErrorCategory])) {
        $safePowerShellCategory = $powerShellCategory
    }
    $exception = $ErrorRecord.Exception
    for ($depth = 0; $exception -and $depth -lt 5; $depth++) {
        $typeName = [string]$exception.GetType().FullName
        if ($typeName -match '^[A-Za-z_][A-Za-z0-9_.`+]{0,199}$') {
            if (-not $safeExceptionType) { $safeExceptionType = $typeName }
        }
        if ($typeName -match '^System\.Management\.Automation\.[A-Za-z0-9_.`+]{1,160}$') {
            $hasPowerShellException = $true
        }

        # AzPS* exceptions expose ErrorKind as a closed enum. Report only its
        # member name, never arbitrary exception metadata or message text.
        if (-not $safeAzErrorKind) {
            $errorKindProperty = $exception.PSObject.Properties['ErrorKind']
            if ($errorKindProperty -and $errorKindProperty.Value -is [System.Enum]) {
                $errorKindType = $errorKindProperty.Value.GetType().FullName
                $errorKindName = [string]$errorKindProperty.Value.ToString()
                if ($errorKindType -match '^Microsoft\.Azure\.Commands\.Common(\.[A-Za-z0-9_.]+)?\.ErrorKind$' -and
                    $errorKindName -match '^[A-Za-z][A-Za-z0-9_]{0,79}$') {
                    $safeAzErrorKind = $errorKindName
                }
            }
        }

        if (-not $safeHttpStatusCode) {
            $statusProperty = $exception.PSObject.Properties['StatusCode']
            if (-not $statusProperty -and $exception.PSObject.Properties['Response']) {
                $responseObject = $exception.PSObject.Properties['Response'].Value
                if ($responseObject) { $statusProperty = $responseObject.PSObject.Properties['StatusCode'] }
            }
            if ($statusProperty -and $null -ne $statusProperty.Value) {
                $statusValue = 0
                $statusParsed = $false
                if ($statusProperty.Value -is [System.Enum]) {
                    $statusValue = [int]$statusProperty.Value
                    $statusParsed = $true
                } else {
                    $statusParsed = [int]::TryParse([string]$statusProperty.Value, [ref]$statusValue)
                }
                if ($statusParsed -and $statusValue -ge 100 -and $statusValue -le 599) {
                    $safeHttpStatusCode = $statusValue
                }
            }
        }

        foreach ($propertyName in @('Code', 'ErrorCode')) {
            $property = $exception.PSObject.Properties[$propertyName]
            if ($property -and $property.Value -is [string]) {
                $candidate = [string]$property.Value
                if ($codeCategories.ContainsKey($candidate)) {
                    $recognizedCode = $candidate
                    $category = $codeCategories[$candidate]
                    break
                }
            }
        }
        if ($recognizedCode) { break }
        $exception = $exception.InnerException
    }

    # ErrorDetails.Message can contain a response body. Parse JSON only in memory,
    # traverse only properties named "code", and emit only exact allowlist hits.
    if (-not $recognizedCode -and $ErrorRecord.ErrorDetails -and
        $ErrorRecord.ErrorDetails.Message -is [string] -and
        $ErrorRecord.ErrorDetails.Message.Length -le 1048576) {
        try {
            $structuredError = ConvertFrom-Json -InputObject $ErrorRecord.ErrorDetails.Message -ErrorAction Stop
            $pending = [System.Collections.Generic.Stack[object]]::new()
            $pending.Push($structuredError)
            $visited = 0
            while ($pending.Count -gt 0 -and $visited -lt 1000) {
                $node = $pending.Pop()
                $visited++
                if ($null -eq $node) { continue }
                if ($node -is [System.Array]) {
                    foreach ($entry in $node) { $pending.Push($entry) }
                    continue
                }
                if ($node -is [System.Collections.IDictionary]) {
                    foreach ($key in $node.Keys) {
                        $value = $node[$key]
                        if ([string]$key -ieq 'code' -and $value -is [string] -and $codeCategories.ContainsKey([string]$value)) {
                            $recognizedCode = [string]$value
                            $category = $codeCategories[$recognizedCode]
                            break
                        }
                        if ($value -is [System.Collections.IDictionary] -or
                            $value -is [System.Array] -or
                            $value -is [System.Management.Automation.PSCustomObject]) {
                            $pending.Push($value)
                        }
                    }
                } elseif ($node -is [System.Management.Automation.PSCustomObject]) {
                    foreach ($property in $node.PSObject.Properties) {
                        if ($property.Name -ieq 'code' -and $property.Value -is [string] -and $codeCategories.ContainsKey([string]$property.Value)) {
                            $recognizedCode = [string]$property.Value
                            $category = $codeCategories[$recognizedCode]
                            break
                        }
                        if ($property.Value -is [System.Collections.IDictionary] -or
                            $property.Value -is [System.Array] -or
                            $property.Value -is [System.Management.Automation.PSCustomObject]) {
                            $pending.Push($property.Value)
                        }
                    }
                }
                if ($recognizedCode) { break }
            }
        } catch {
            # Malformed or non-JSON details are intentionally ignored.
        }
    }

    # Az PowerShell wraps some service failures in AzPS* exceptions and exposes a
    # specifically desensitized message. Treat it only as an in-memory JSON
    # candidate; never display it or parse arbitrary message text for a code.
    if (-not $recognizedCode) {
        $exception = $ErrorRecord.Exception
        for ($depth = 0; $exception -and $depth -lt 5 -and -not $recognizedCode; $depth++) {
            $desensitizedProperty = $exception.PSObject.Properties['DesensitizedErrorMessage']
            if ($desensitizedProperty -and $desensitizedProperty.Value -is [string] -and
                $desensitizedProperty.Value.Length -le 1048576) {
                try {
                    $structuredError = ConvertFrom-Json -InputObject $desensitizedProperty.Value -ErrorAction Stop
                    $pending = [System.Collections.Generic.Stack[object]]::new()
                    $pending.Push($structuredError)
                    $visited = 0
                    while ($pending.Count -gt 0 -and $visited -lt 1000) {
                        $node = $pending.Pop()
                        $visited++
                        if ($null -eq $node) { continue }
                        if ($node -is [System.Array]) {
                            foreach ($entry in $node) { $pending.Push($entry) }
                            continue
                        }
                        if ($node -is [System.Collections.IDictionary]) {
                            foreach ($key in $node.Keys) {
                                $value = $node[$key]
                                if ([string]$key -ieq 'code' -and $value -is [string] -and $codeCategories.ContainsKey([string]$value)) {
                                    $recognizedCode = [string]$value
                                    $category = $codeCategories[$recognizedCode]
                                    break
                                }
                                if ($value -is [System.Collections.IDictionary] -or
                                    $value -is [System.Array] -or
                                    $value -is [System.Management.Automation.PSCustomObject]) {
                                    $pending.Push($value)
                                }
                            }
                        } elseif ($node -is [System.Management.Automation.PSCustomObject]) {
                            foreach ($property in $node.PSObject.Properties) {
                                if ($property.Name -ieq 'code' -and $property.Value -is [string] -and $codeCategories.ContainsKey([string]$property.Value)) {
                                    $recognizedCode = [string]$property.Value
                                    $category = $codeCategories[$recognizedCode]
                                    break
                                }
                                if ($property.Value -is [System.Collections.IDictionary] -or
                                    $property.Value -is [System.Array] -or
                                    $property.Value -is [System.Management.Automation.PSCustomObject]) {
                                    $pending.Push($property.Value)
                                }
                            }
                        }
                        if ($recognizedCode) { break }
                    }
                } catch {
                    # Non-JSON or malformed desensitized metadata is not classified.
                }
            }
            $exception = $exception.InnerException
        }
    }

    if (-not $recognizedCode -and $hasPowerShellException) {
        $category = 'client-side PowerShell failure'
    }
    if (-not $recognizedCode -and $safeExceptionType -match '^System\.Net\.') {
        $category = 'network/connectivity failure'
    }
    if (-not $recognizedCode -and $safeHttpStatusCode) {
        $category = switch ($safeHttpStatusCode) {
            { $_ -in 401, 403 } { 'authorization failure'; break }
            404 { 'resource not found'; break }
            429 { 'throttling'; break }
            { $_ -in 408, 500, 502, 503, 504 } { 'network/connectivity failure'; break }
            default { $category }
        }
    }

    [pscustomobject]@{
        Code             = $recognizedCode
        Category         = $category
        ExceptionType    = $safeExceptionType
        PowerShellCategory = $safePowerShellCategory
        HttpStatusCode   = $safeHttpStatusCode
        AzErrorKind      = $safeAzErrorKind
    }
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
    if ($headerValue -is [System.Array]) {
        if ($headerValue.Length -gt 0) { return [string]$headerValue[0] }
        return $null
    }
    if ($null -ne $headerValue) { return [string]$headerValue }
    return $null
}

function Invoke-ApiResourceGroupDeployment {
    param(
        [Parameter(Mandatory)][string]$DeploymentName,
        [Parameter(Mandatory)][hashtable]$Parameters,
        [Parameter(Mandatory)][hashtable]$TemplateObject,
        [Parameter(Mandatory)][string]$SubscriptionId
    )

    $adminPlaintext = $null
    $jwtPlaintext = $null
    $deploymentParameters = $null
    $deploymentBody = $null
    $deploymentPayload = $null
    $response = $null
    $responseJson = $null
    $deploymentResult = $null
    $failureStage = 'building in-memory ARM deployment request'
    $safeFailureReported = $false
    try {
        # Use the ARM REST deployment endpoint because the installed Az.Resources
        # deployment cmdlet failed during dynamic-parameter binding.
        # The JSON body exists only in memory and is never written or displayed.
        $adminPlaintext = ConvertFrom-SecureStringForArmPayload -SecureValue $Parameters.apiAdminSeedPassword
        $jwtPlaintext = ConvertFrom-SecureStringForArmPayload -SecureValue $Parameters.apiJwtSigningKey
        $deploymentParameters = @{
            imageTag = @{ value = [string]$Parameters.imageTag }
            revisionSuffix = @{ value = [string]$Parameters.revisionSuffix }
            apiJwtSigningKey = @{ value = $jwtPlaintext }
            apiAdminSeedPassword = @{ value = $adminPlaintext }
        }
        $deploymentBody = @{
            properties = @{
                mode = 'Incremental'
                template = $TemplateObject
                parameters = $deploymentParameters
            }
        }
        $deploymentPayload = ConvertTo-Json -InputObject $deploymentBody -Depth 100 -Compress -ErrorAction Stop
        $deploymentUri = [uri]("https://management.azure.com/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/{2}?api-version=2025-04-01" -f `
            $SubscriptionId, [uri]::EscapeDataString($resourceGroupName), [uri]::EscapeDataString($DeploymentName))

        $failureStage = 'submitting ARM resource-group deployment'
        $response = Invoke-AzRestMethod -Uri $deploymentUri -Method PUT -Payload $deploymentPayload -ErrorAction Stop
        $statusCode = [int]$response.StatusCode
        if ($statusCode -notin @(200, 201, 202)) {
            $diagnosticRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.Exception]::new('ARM deployment request returned a non-success status.'),
                'ArmDeploymentHttpFailure',
                [System.Management.Automation.ErrorCategory]::InvalidOperation,
                $null
            )
            if ([string]$response.Content) {
                $diagnosticRecord.ErrorDetails = [System.Management.Automation.ErrorDetails]::new([string]$response.Content)
            }
            $diagnostic = Get-SafeWhatIfFailureDiagnostic -ErrorRecord $diagnosticRecord
            Write-Host ("ARM deployment request returned HTTP {0}; error code: {1}; category: {2}." -f `
                $statusCode, $(if ($diagnostic.Code) { $diagnostic.Code } else { 'unrecognized' }), $diagnostic.Category) -ForegroundColor Yellow
            $safeFailureReported = $true
            throw 'ARM did not accept the API deployment request. No automatic retry was attempted.'
        }

        $pollAttempt = 0
        do {
            if ([int]$response.StatusCode -eq 202 -or $pollAttempt -gt 0) {
                if ($pollAttempt -gt 0) { Start-Sleep -Seconds 5 }
                $failureStage = 'checking ARM deployment provisioning state'
                $response = Invoke-AzRestMethod -Uri $deploymentUri -Method GET -ErrorAction Stop
                if ([int]$response.StatusCode -ne 200) {
                    throw 'ARM deployment status could not be read. Do not retry; inspect the deployment record.'
                }
            }
            if ([string]::IsNullOrWhiteSpace([string]$response.Content)) {
                throw 'ARM returned no inspectable deployment status. Do not retry; inspect the deployment record.'
            }
            try {
                $responseJson = ConvertFrom-Json -InputObject ([string]$response.Content) -ErrorAction Stop
                $deploymentResult = ConvertTo-TemplateHashtable -InputObject $responseJson
                $responseJson = $null
            } catch {
                throw 'ARM returned malformed deployment status. Do not retry; inspect the deployment record.'
            }
            if (-not $deploymentResult -or -not ($deploymentResult.properties -is [System.Collections.IDictionary]) -or
                [string]$deploymentResult.name -cne $DeploymentName -or
                [string]$deploymentResult.properties.provisioningState -notin @('Accepted', 'Running', 'Creating', 'Succeeded', 'Failed', 'Canceled', 'Cancelled')) {
                throw 'ARM returned an incomplete or unrecognized deployment status. Do not retry; inspect the deployment record.'
            }
            $state = [string]$deploymentResult.properties.provisioningState
            if ($state -in @('Succeeded', 'Failed', 'Canceled', 'Cancelled')) { break }
            $pollAttempt++
        } while ($pollAttempt -lt 180)

        if ($state -notin @('Succeeded', 'Failed', 'Canceled', 'Cancelled')) {
            throw 'ARM deployment is still processing after the bounded wait. Do not resubmit; inspect the deployment record.'
        }
        if ($state -ne 'Succeeded') {
            $errorJson = $null
            $diagnosticRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.Exception]::new('ARM deployment provisioning failed.'),
                'ArmDeploymentProvisioningFailure',
                [System.Management.Automation.ErrorCategory]::InvalidOperation,
                $null
            )
            if ($deploymentResult.properties.error) {
                $errorJson = ConvertTo-Json -InputObject $deploymentResult.properties.error -Depth 20 -Compress
                $diagnosticRecord.ErrorDetails = [System.Management.Automation.ErrorDetails]::new($errorJson)
            }
            $diagnostic = Get-SafeWhatIfFailureDiagnostic -ErrorRecord $diagnosticRecord
            Write-Host ("ARM deployment state: {0}; error code: {1}; category: {2}." -f `
                $state, $(if ($diagnostic.Code) { $diagnostic.Code } else { 'unrecognized' }), $diagnostic.Category) -ForegroundColor Yellow
            $safeFailureReported = $true
            throw 'ARM API deployment did not succeed. Do not automatically retry; inspect deployment operations.'
        }

        return [pscustomobject]@{
            DeploymentName  = [string]$deploymentResult.name
            ProvisioningState = $state
            CorrelationId   = [string]$deploymentResult.properties.correlationId
        }
    } catch {
        if (-not $safeFailureReported) {
            $diagnostic = Get-SafeWhatIfFailureDiagnostic -ErrorRecord $_
            Write-Host ("Safe deployment failure stage: {0}." -f $failureStage) -ForegroundColor DarkGray
            if ($diagnostic.Code) {
                Write-Host ("Recognized Azure error code: {0}; category: {1}." -f $diagnostic.Code, $diagnostic.Category) -ForegroundColor Yellow
            } else {
                Write-Host ("No recognized Azure error code; category: {0}." -f $diagnostic.Category) -ForegroundColor Yellow
            }
            if ($diagnostic.HttpStatusCode) { Write-Host ("HTTP status: {0}." -f $diagnostic.HttpStatusCode) -ForegroundColor DarkGray }
            if ($diagnostic.ExceptionType) { Write-Host ("Exception type: {0}." -f $diagnostic.ExceptionType) -ForegroundColor DarkGray }
        }
        throw 'ARM API deployment failed or remains unverified. No automatic retry was attempted.'
    } finally {
        if ($deploymentParameters) {
            foreach ($parameterValue in $deploymentParameters.Values) {
                if ($parameterValue -is [System.Collections.IDictionary]) { $parameterValue.Clear() }
            }
            $deploymentParameters.Clear()
        }
        if ($deploymentBody -and $deploymentBody.properties -is [System.Collections.IDictionary]) {
            $deploymentBody.properties.Clear()
            $deploymentBody.Clear()
        }
        $adminPlaintext = $null
        $jwtPlaintext = $null
        $deploymentPayload = $null
        $deploymentUri = $null
        $response = $null
        $responseJson = $null
        $deploymentResult = $null
        $errorJson = $null
        $diagnosticRecord = $null
        $diagnostic = $null
    }
}

function Invoke-VerifiedApiWhatIf {
    param(
        [Parameter(Mandatory)][string]$DeploymentName,
        [Parameter(Mandatory)][hashtable]$Parameters,
        [Parameter(Mandatory)][hashtable]$TemplateObject,
        [Parameter(Mandatory)][string]$SubscriptionId,
        [Parameter(Mandatory)][string]$ExpectedApiResourceId
    )

    $adminPlaintext = $null
    $jwtPlaintext = $null
    $requestParameters = $null
    $requestBody = $null
    $requestPayload = $null
    $readCheckResponse = $null
    $response = $null
    $pollResponse = $null
    $pollUri = $null
    $locationHeader = $null
    $retryAfterHeader = $null
    $result = $null
    $safeFailureAlreadyReported = $false
    $failureStage = 'building in-memory request'
    try {
    try {
        # Confirm this Az context can issue an authenticated ARM REST read before
        # constructing or submitting the secret-bearing What-If request.
        $failureStage = 'verifying ARM REST read access'
        $resourceGroupUri = [uri]("https://management.azure.com/subscriptions/{0}/resourcegroups/{1}?api-version=2021-04-01" -f $SubscriptionId, [uri]::EscapeDataString($resourceGroupName))
        $readCheckResponse = Invoke-AzRestMethod -Uri $resourceGroupUri -Method GET -ErrorAction Stop
        $readCheckStatus = [int]$readCheckResponse.StatusCode
        if ($readCheckStatus -ne 200) {
            $readCheckCategory = switch ($readCheckStatus) {
                { $_ -in 401, 403 } { 'authorization failure'; break }
                404 { 'resource not found'; break }
                429 { 'throttling'; break }
                { $_ -in 408, 500, 502, 503, 504 } { 'network/connectivity failure'; break }
                default { 'unclassified failure' }
            }
            $readCheckCode = $null
            $readCheckCodeCategories = @{
                InvalidApiVersionParameter = 'request validation failure'
                MissingApiVersionParameter = 'request validation failure'
                InvalidRequestContent = 'request validation failure'
                InvalidSubscriptionId = 'request validation failure'
                SubscriptionNotFound = 'resource not found'
                ResourceGroupNotFound = 'resource not found'
                AuthorizationFailed = 'authorization failure'
                InvalidAuthenticationToken = 'authorization failure'
                TooManyRequests = 'throttling'
                RequestTimeout = 'network/connectivity failure'
                ServiceUnavailable = 'network/connectivity failure'
            }
            try {
                $readError = ConvertFrom-Json -InputObject ([string]$readCheckResponse.Content) -ErrorAction Stop
                $candidateReadCode = [string]$readError.error.code
                if ($readCheckCodeCategories.ContainsKey($candidateReadCode)) {
                    $readCheckCode = $candidateReadCode
                    $readCheckCategory = $readCheckCodeCategories[$candidateReadCode]
                }
            } catch {
                # The response body is intentionally suppressed if not recognized.
            }
            if ($readCheckCode) {
                Write-Host 'Safe failure stage: verifying ARM REST read access.' -ForegroundColor DarkGray
                Write-Host ("ARM REST read check returned HTTP {0}; recognized Azure error code: {1}; failure category: {2}." -f $readCheckStatus, $readCheckCode, $readCheckCategory) -ForegroundColor Yellow
            } else {
                Write-Host 'Safe failure stage: verifying ARM REST read access.' -ForegroundColor DarkGray
                Write-Host ("ARM REST read check returned HTTP {0}; no recognized Azure error code; failure category: {1}." -f $readCheckStatus, $readCheckCategory) -ForegroundColor Yellow
            }
            $safeFailureAlreadyReported = $true
            throw 'ARM REST read check did not return HTTP 200. No What-If request or deployment was submitted.'
        }
        Write-Host 'ARM REST read check succeeded (HTTP 200).'

        $adminPlaintext = ConvertFrom-SecureStringForArmPayload -SecureValue $Parameters.apiAdminSeedPassword
        $jwtPlaintext = ConvertFrom-SecureStringForArmPayload -SecureValue $Parameters.apiJwtSigningKey
        $requestParameters = @{
            imageTag = @{ value = [string]$Parameters.imageTag }
            revisionSuffix = @{ value = [string]$Parameters.revisionSuffix }
            apiJwtSigningKey = @{ value = $jwtPlaintext }
            apiAdminSeedPassword = @{ value = $adminPlaintext }
        }
        $requestBody = @{
            properties = @{
                mode = 'Incremental'
                template = $TemplateObject
                parameters = $requestParameters
                whatIfSettings = @{ resultFormat = 'FullResourcePayloads' }
            }
        }
        # ARM requires secureString parameter values in the JSON request body.
        # Serialize only in memory; never emit, persist, or trace this payload.
        $failureStage = 'serializing in-memory request'
        $requestPayload = ConvertTo-Json -InputObject $requestBody -Depth 100 -Compress -ErrorAction Stop
        $whatIfUri = [uri]("https://management.azure.com/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/{2}/whatIf?api-version=2025-04-01" -f $SubscriptionId, [uri]::EscapeDataString($resourceGroupName), [uri]::EscapeDataString($DeploymentName))
        $failureStage = 'submitting ARM What-If request'
        # Inspect the initial response directly. ARM's 202 What-If response uses a
        # relative Location header, so resolve and validate it before polling.
        $response = Invoke-AzRestMethod -Uri $whatIfUri -Method POST -Payload $requestPayload -ErrorAction Stop
        if ([int]$response.StatusCode -eq 202) {
            $locationHeader = Get-ArmResponseHeaderValue -Response $response -Name 'Location'
            $retryAfterHeader = Get-ArmResponseHeaderValue -Response $response -Name 'Retry-After'
            if ([string]::IsNullOrWhiteSpace($locationHeader)) {
                throw 'ARM accepted What-If without a Location header. No deployment was submitted.'
            }
            $locationUri = $null
            if (-not [uri]::TryCreate($locationHeader, [System.UriKind]::Absolute, [ref]$locationUri)) {
                $locationUri = [uri]::new($whatIfUri, $locationHeader)
            }
            if ($locationUri.Scheme -ne 'https' -or $locationUri.Host -ine $whatIfUri.Host -or
                $locationUri.Port -ne $whatIfUri.Port) {
                throw 'ARM What-If returned an unexpected polling URI. No deployment was submitted.'
            }
            $pollUri = $locationUri
            $pollAttempt = 0
            while ([int]$response.StatusCode -eq 202 -and $pollAttempt -lt 180) {
                $delaySeconds = 5
                $parsedDelay = 0
                if ([int]::TryParse([string]$retryAfterHeader, [ref]$parsedDelay) -and $parsedDelay -ge 1) {
                    $delaySeconds = [Math]::Min($parsedDelay, 30)
                }
                Start-Sleep -Seconds $delaySeconds
                $failureStage = 'polling asynchronous ARM What-If'
                $pollResponse = Invoke-AzRestMethod -Uri $pollUri -Method GET -ErrorAction Stop
                $response = $pollResponse
                $pollResponse = $null
                $pollAttempt++
                if ([int]$response.StatusCode -eq 202) {
                    $nextRetryAfter = Get-ArmResponseHeaderValue -Response $response -Name 'Retry-After'
                    if ($nextRetryAfter) { $retryAfterHeader = $nextRetryAfter }
                }
            }
            if ([int]$response.StatusCode -eq 202) {
                throw 'ARM What-If polling exceeded the bounded wait period. No deployment was submitted.'
            }
        }

        $failureStage = 'processing completed ARM response'
        $statusCode = [int]$response.StatusCode
        if ($statusCode -notin @(200)) {
            $httpErrorCode = $null
            $httpCategory = switch ($statusCode) {
                { $_ -in 401, 403 } { 'authorization failure'; break }
                404 { 'resource not found'; break }
                429 { 'throttling'; break }
                { $_ -in 408, 500, 502, 503, 504 } { 'network/connectivity failure'; break }
                default { 'unclassified failure' }
            }
            try {
                $errorContent = ConvertFrom-Json -InputObject ([string]$response.Content) -ErrorAction Stop
                $candidateCode = [string]$errorContent.error.code
                if ($candidateCode -match '^[A-Za-z][A-Za-z0-9]{1,63}$' -and $candidateCode -in @('AuthorizationFailed','Forbidden','Unauthorized','InvalidAuthenticationToken','InvalidTemplate','InvalidTemplateDeployment','DeploymentTemplateValidationFailed','InvalidParameter','ResourceNotFound','ResourceGroupNotFound','TooManyRequests','Throttled','RateLimitExceeded','RequestTimeout','GatewayTimeout','ServiceUnavailable','NameResolutionFailure','ConnectFailure')) {
                    $httpErrorCode = $candidateCode
                }
            } catch { }
            if ($httpErrorCode) {
                $httpCategory = switch ($httpErrorCode) {
                    { $_ -in 'AuthorizationFailed','Forbidden','Unauthorized','InvalidAuthenticationToken' } { 'authorization failure'; break }
                    { $_ -in 'InvalidTemplate','InvalidTemplateDeployment','DeploymentTemplateValidationFailed','InvalidParameter' } { 'template validation failure'; break }
                    { $_ -in 'ResourceNotFound','ResourceGroupNotFound' } { 'resource not found'; break }
                    { $_ -in 'TooManyRequests','Throttled','RateLimitExceeded' } { 'throttling'; break }
                    { $_ -in 'RequestTimeout','GatewayTimeout','ServiceUnavailable','NameResolutionFailure','ConnectFailure' } { 'network/connectivity failure'; break }
                    default { $httpCategory }
                }
                Write-Host ("Recognized Azure error code: {0}; failure category: {1}." -f $httpErrorCode, $httpCategory) -ForegroundColor Yellow
            } else {
                Write-Host ("No recognized Azure error code was available; failure category: {0}." -f $httpCategory) -ForegroundColor Yellow
            }
            throw 'Azure REST What-If did not return a successful response. No deployment was submitted.'
        }

        try {
            $failureStage = 'parsing ARM What-If response'
            $resultJson = ConvertFrom-Json -InputObject ([string]$response.Content) -ErrorAction Stop
            $result = ConvertTo-TemplateHashtable -InputObject $resultJson
            $resultJson = $null
        } catch { throw 'Azure REST What-If returned malformed JSON. No deployment was submitted.' }
        if (-not $result -or [string]$result.status -ne 'Succeeded' -or
            -not ($result.properties -is [System.Collections.IDictionary]) -or
            -not ($result.properties.changes -is [System.Collections.IEnumerable])) {
            throw 'Azure REST What-If did not return a completed, inspectable changes collection. No deployment was submitted.'
        }
    } catch {
        # Never emit messages, ErrorDetails, request bodies, parameters, or nested
        # exception text. Only the classifier's fixed categories and allowlisted code
        # can be written to the terminal.
        if (-not $safeFailureAlreadyReported) {
            $diagnostic = Get-SafeWhatIfFailureDiagnostic -ErrorRecord $_
            Write-Host ("Safe failure stage: {0}." -f $failureStage) -ForegroundColor DarkGray
            if ($diagnostic.Code) {
                Write-Host ("Recognized Azure error code: {0}; failure category: {1}." -f $diagnostic.Code, $diagnostic.Category) -ForegroundColor Yellow
            } else {
                Write-Host ("No recognized Azure error code was available; failure category: {0}." -f $diagnostic.Category) -ForegroundColor Yellow
            }
            if ($diagnostic.ExceptionType) {
                Write-Host ("Exception type: {0}." -f $diagnostic.ExceptionType) -ForegroundColor DarkGray
            }
            if ($diagnostic.PowerShellCategory) {
                Write-Host ("PowerShell error category: {0}." -f $diagnostic.PowerShellCategory) -ForegroundColor DarkGray
            }
            if ($diagnostic.HttpStatusCode) {
                Write-Host ("HTTP status code: {0}." -f $diagnostic.HttpStatusCode) -ForegroundColor DarkGray
            }
            if ($diagnostic.AzErrorKind) {
                Write-Host ("Az PowerShell error kind: {0}." -f $diagnostic.AzErrorKind) -ForegroundColor DarkGray
            }
        }
        throw 'Azure preflight failed before returning inspectable What-If changes. No deployment was submitted.'
    }

    $changeRows = @(
        foreach ($change in @($result.properties.changes)) {
            if (-not $change -or [string]$change.changeType -notmatch '^(Create|Delete|Ignore|NoChange|Deploy|Modify|Unsupported)$' -or
                [string]$change.resourceId -notmatch '^/subscriptions/[0-9a-fA-F-]{36}/resourceGroups/[A-Za-z0-9_.()-]+/providers/[A-Za-z0-9.]+/[A-Za-z0-9./_-]+$') {
                throw 'Azure REST What-If returned an uninspectable resource change. No deployment was submitted.'
            }
            $deltaSummaries = @()
            if ([string]$change.changeType -eq 'Modify') {
                if (-not ($change.delta -is [System.Collections.IEnumerable]) -or
                    $change.delta -is [string] -or $change.delta -is [System.Collections.IDictionary] -or
                    @($change.delta).Count -eq 0) {
                    throw 'Azure REST What-If did not include inspectable property deltas for a modification. No deployment was submitted.'
                }
                $deltaSummaries = @(Get-WhatIfPropertyDeltaSummaries -Delta $change.delta)
                if ($deltaSummaries.Count -eq 0) {
                    throw 'Azure REST What-If returned an empty property delta for a modification. No deployment was submitted.'
                }
            } elseif ($change.Contains('delta') -and $null -ne $change.delta -and
                [string]$change.changeType -notin @('Ignore', 'NoChange')) {
                # Any actionable non-Modify is rejected by the gate below. Do not
                # inspect or expose its payload while preparing the sanitized summary.
            }
            [pscustomobject]@{
                ChangeType = [string]$change.changeType
                ResourceId = [string]$change.resourceId
                DeltaSummaries = $deltaSummaries
            }
        }
    )
    if ($changeRows.Count -eq 0) {
        throw 'Azure what-if returned no inspectable resource changes. No deployment was submitted.'
    }

    Write-Host 'Proposed resource changes (resource IDs and sanitized property deltas):'
    foreach ($change in $changeRows) {
        Write-Host ('  {0}  {1}' -f [string]$change.ChangeType, [string]$change.ResourceId)
        foreach ($delta in @($change.DeltaSummaries)) {
            Write-Host ('    {0}  [{1}]' -f [string]$delta.Path, [string]$delta.PropertyChangeType)
        }
    }

    $actionableChanges = @($changeRows | Where-Object { [string]$_.ChangeType -notin @('Ignore', 'NoChange') })
    $apiChanges = @($actionableChanges | Where-Object {
        [string]$_.ResourceId -ieq $ExpectedApiResourceId
    })
    $unrelatedChanges = @($actionableChanges | Where-Object {
        [string]$_.ResourceId -ine $ExpectedApiResourceId
    })
    if ($actionableChanges.Count -ne 1 -or
        $apiChanges.Count -ne 1 -or
        $unrelatedChanges.Count -ne 0 -or
        [string]$apiChanges[0].ChangeType -ne 'Modify') {
        throw 'What-if did not show exactly one modification to the intended API app and zero other actionable changes. No deployment was submitted.'
    }
    $unexpectedPaths = @($apiChanges[0].DeltaSummaries | Where-Object {
        -not (Test-ExpectedApiWhatIfPropertyPath -Path ([string]$_.Path) `
            -PropertyChangeType ([string]$_.PropertyChangeType) -HasChildren ([bool]$_.HasChildren))
    })
    if ($unexpectedPaths.Count -gt 0) {
        Write-Host 'Unexpected API property paths (values withheld):' -ForegroundColor Yellow
        foreach ($delta in $unexpectedPaths) {
            Write-Host ('  {0}  [{1}]' -f [string]$delta.Path, [string]$delta.PropertyChangeType)
        }
        throw 'What-if included API property paths outside the reviewed update. No deployment was submitted.'
    }
    return $changeRows
    } finally {
        if ($requestParameters) {
            foreach ($parameterValue in $requestParameters.Values) {
                if ($parameterValue -is [System.Collections.IDictionary]) { $parameterValue.Clear() }
            }
            $requestParameters.Clear()
        }
        if ($requestBody -and $requestBody.properties -is [System.Collections.IDictionary]) {
            $requestBody.properties.Clear()
            $requestBody.Clear()
        }
        $adminPlaintext = $null
        $jwtPlaintext = $null
        $requestPayload = $null
        $errorContent = $null
        $candidateCode = $null
        $whatIfUri = $null
        $resourceGroupUri = $null
        $pollUri = $null
        $locationHeader = $null
        $retryAfterHeader = $null
        $readCheckResponse = $null
        $pollResponse = $null
        $response = $null
        $result = $null
        $resultJson = $null
    }
}

try {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    $templateFile = Join-Path $repoRoot 'infra\portfolio-demo-api.bicep'
    if (-not (Test-Path -LiteralPath $templateFile -PathType Leaf)) {
        throw 'The isolated API Bicep template was not found. No deployment was submitted.'
    }

    $resourcesModule = Get-Module -ListAvailable -Name Az.Resources |
        Sort-Object { [version]$_.Version } -Descending |
        Select-Object -First 1
    if (-not $resourcesModule) {
        throw 'Az.Resources is required for read-only Azure resource checks. No password has been requested and no deployment was submitted.'
    }

    Import-Module Az.Resources -RequiredVersion $resourcesModule.Version -ErrorAction Stop
    Write-Host ("Using Az.Resources version: {0}" -f $resourcesModule.Version)
    $restCommand = Get-Command Invoke-AzRestMethod -ErrorAction Stop
    foreach ($requiredParameter in @('Uri', 'Method', 'Payload')) {
        if (-not $restCommand.Parameters.ContainsKey($requiredParameter)) {
            throw "The installed Invoke-AzRestMethod cmdlet lacks required parameter '$requiredParameter'. No password has been requested and no deployment was submitted."
        }
    }

    $context = Get-AzContext -ErrorAction Stop
    if (-not $context -or $context.Subscription.Name -ne 'Azure subscription 1') {
        throw 'The active Azure context is not Azure subscription 1. Select the intended subscription and rerun. No password has been requested and no deployment was submitted.'
    }
    $subscription = Get-AzSubscription -SubscriptionId $context.Subscription.Id -ErrorAction Stop
    if (-not $subscription -or $subscription.Name -ne 'Azure subscription 1' -or $subscription.State -ne 'Enabled') {
        throw 'The active Azure subscription is not the expected enabled subscription. No password has been requested and no deployment was submitted.'
    }

    $resourceGroup = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction Stop
    if (($resourceGroup.Location -replace '\s', '').ToLowerInvariant() -ne 'centralus') {
        throw 'The target resource group is not in Central US. No password has been requested and no deployment was submitted.'
    }

    $appResourceId = "/subscriptions/$($subscription.Id)/resourceGroups/$resourceGroupName/providers/Microsoft.App/containerApps/$apiAppName"
    $appResource = Get-AzResource -ResourceId $appResourceId -ExpandProperties -ErrorAction Stop
    $appProperties = $appResource.Properties
    $environmentIdSuffix = "/managedEnvironments/cae-er-portfolio-demo"
    if ($appResource.Name -ne $apiAppName -or
        (([string]$appResource.Location -replace '\s', '').ToLowerInvariant() -ne 'centralus') -or
        -not ([string]$appProperties.managedEnvironmentId).EndsWith($environmentIdSuffix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The existing Container App or its Central US Container Apps environment does not match the expected target. No password has been requested and no deployment was submitted.'
    }

    $registryResourceId = "/subscriptions/$($subscription.Id)/resourceGroups/$registryResourceGroupName/providers/Microsoft.ContainerRegistry/registries/$registryName"
    $registryResource = Get-AzResource -ResourceId $registryResourceId -ExpandProperties -ErrorAction Stop
    if (-not $registryResource -or $registryResource.Name -ne $registryName -or
        [string]$registryResource.Properties.loginServer -ne $registryLoginServer) {
        throw 'The East US 2 registry resource or login server does not match the expected target. No password has been requested and no deployment was submitted.'
    }
    $getAccessTokenCommand = Get-Command Get-AzAccessToken -ErrorAction Stop
    if (-not $getAccessTokenCommand.Parameters.ContainsKey('AsSecureString')) {
        throw 'The installed Az.Accounts module cannot provide a secure ACR access token. Update Az.Accounts before continuing; no password has been requested and no deployment was submitted.'
    }
    $registryAccessToken = $null
    $apiAcrAccessToken = $null
    $acrFailureStage = 'requesting Azure access token'
    try {
        $registryAccessToken = Get-AzAccessToken -ResourceUrl 'https://containerregistry.azure.net' -AsSecureString -ErrorAction Stop
        if (-not $registryAccessToken -or $registryAccessToken.Token -isnot [System.Security.SecureString]) {
            throw 'The installed Az.Accounts module did not return a secure ACR access token.'
        }
        $acrFailureStage = 'exchanging API repository credentials and reading tag metadata'
        $apiAcrAccessToken = Get-AcrRepositoryAccessToken -LoginServer $registryLoginServer -Repository $repositoryName -TenantId $context.Tenant.Id -AzureAccessToken $registryAccessToken.Token
        $apiTagResult = Test-AcrRepositoryTag -LoginServer $registryLoginServer -Repository $repositoryName -Tag $imageTag -AccessToken $apiAcrAccessToken
        $apiAcrAccessToken.Dispose()
        $apiAcrAccessToken = $null
    } catch {
        $acrErrorRecord = $_
        $statusCode = 0
        $responseProperty = $acrErrorRecord.Exception.PSObject.Properties['Response']
        if ($responseProperty -and $responseProperty.Value) {
            $statusProperty = $responseProperty.Value.PSObject.Properties['StatusCode']
            if ($statusProperty -and $null -ne $statusProperty.Value) {
                if ($statusProperty.Value -is [System.Enum]) { $statusCode = [int]$statusProperty.Value }
                else { [void][int]::TryParse([string]$statusProperty.Value, [ref]$statusCode) }
            }
        }
        $failureCategory = switch ($statusCode) {
            { $_ -in 401, 403 } { 'authorization failure'; break }
            429 { 'throttling'; break }
            { $_ -in 408, 500, 502, 503, 504 } { 'network/connectivity failure'; break }
            { $_ -ge 400 -and $_ -le 499 } { 'request failure'; break }
            default {
                if ([string]$acrErrorRecord.Exception.GetType().FullName -match '^System\.Net\.') { 'network/connectivity failure' }
                else { 'unclassified failure' }
            }
        }
        $exceptionType = [string]$acrErrorRecord.Exception.GetType().FullName
        if ($exceptionType -notmatch '^[A-Za-z_][A-Za-z0-9_.`+]{0,199}$') { $exceptionType = 'unavailable' }
        $safeMissingCommand = $null
        if ($acrErrorRecord.Exception -is [System.Management.Automation.CommandNotFoundException]) {
            $candidateCommand = [string]$acrErrorRecord.Exception.CommandName
            if ($candidateCommand -match '^[A-Za-z_][A-Za-z0-9_.-]{0,127}$') { $safeMissingCommand = $candidateCommand }
        }
        $safeStatus = if ($statusCode -ge 100 -and $statusCode -le 599) { [string]$statusCode } else { 'unavailable' }
        Write-Host ("ACR preflight stage={0}; HTTP={1}; category={2}; exception type={3}." -f $acrFailureStage, $safeStatus, $failureCategory, $exceptionType) -ForegroundColor Yellow
        if ($safeMissingCommand) { Write-Host ("Missing command: {0}." -f $safeMissingCommand) -ForegroundColor DarkGray }
        $safeMissingCommand = $null
        $acrErrorRecord = $null
        throw 'ACR authentication or tag verification failed. No password has been requested and no deployment was submitted.'
    } finally {
        if ($apiAcrAccessToken) { $apiAcrAccessToken.Dispose() }
        if ($registryAccessToken -and $registryAccessToken.Token -is [System.Security.SecureString]) {
            $registryAccessToken.Token.Dispose()
        }
        $registryAccessToken = $null
    }
    if (-not $apiTagResult.Succeeded) {
        $safeHttpStatus = if ([int]$apiTagResult.HttpStatus -ge 100 -and [int]$apiTagResult.HttpStatus -le 599) { [string]$apiTagResult.HttpStatus } else { 'unavailable' }
        Write-Host ("ACR tag metadata request failed; repository={0}; tag={1}; HTTP={2}; category={3}." -f $repositoryName, $imageTag, $safeHttpStatus, $apiTagResult.Category) -ForegroundColor Yellow
        if ($apiTagResult.ExceptionType) {
            Write-Host ("Exception type: {0}." -f $apiTagResult.ExceptionType) -ForegroundColor DarkGray
        }
        throw 'Could not securely verify the API image tag and digest in the East US 2 ACR. No password has been requested and no deployment was submitted.'
    }
    if (-not $apiTagResult.Exists) {
        throw 'The required immutable API image tag is absent from ACR. No password has been requested and no deployment was submitted.'
    }
    if ([string]$apiTagResult.Digest -cne $expectedImageDigest) {
        throw 'The API image tag digest does not match the verified image. No password has been requested and no deployment was submitted.'
    }
    Write-Host ("Verified API image target: {0}" -f $expectedImage)
    Write-Host ("Verified API manifest digest: {0}" -f $apiTagResult.Digest)

    $gitCommand = Get-Command git -ErrorAction Stop
    $priorErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $branchOutput = @(& $gitCommand.Source -C $repoRoot branch --show-current 2>&1)
        $branchExitCode = $LASTEXITCODE
        $diffCheckOutput = @(& $gitCommand.Source -C $repoRoot diff --check 2>&1)
        $diffCheckExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $priorErrorActionPreference
    }
    $currentBranch = [string]($branchOutput | Select-Object -Last 1)
    if ($branchExitCode -ne 0 -or $currentBranch -ne 'docs/phase18-portfolio-release') {
        throw 'The current branch is not docs/phase18-portfolio-release. No password has been requested and no deployment was submitted.'
    }
    if ($diffCheckExitCode -ne 0) {
        throw 'git diff --check failed. Review the working tree before continuing; no password has been requested and no deployment was submitted.'
    }
    $scriptLines = [System.IO.File]::ReadAllLines($PSCommandPath)
    if ($scriptLines | Where-Object { $_ -match '[\t ]+$' } | Select-Object -First 1) {
        throw 'The deployment script contains trailing whitespace. No password has been requested and no deployment was submitted.'
    }

    $azCommand = Get-Command az -ErrorAction Stop
    $compiledTemplateText = (& $azCommand.Source bicep build --file $templateFile --stdout 2>$null | Out-String)
    if ($LASTEXITCODE -ne 0) {
        throw 'Bicep compilation failed. No password has been requested and no deployment was submitted.'
    }
    # Compile and parse ARM JSON in memory for the REST What-If request. No
    # template or secret-bearing request payload is written to disk.
    $compiledTemplateJson = ConvertFrom-Json -InputObject $compiledTemplateText -ErrorAction Stop
    $compiledTemplate = ConvertTo-TemplateHashtable -InputObject $compiledTemplateJson
    $compiledTemplateJson = $null
    foreach ($secureParameterName in @('apiJwtSigningKey', 'apiAdminSeedPassword')) {
        if (-not $compiledTemplate.parameters.$secureParameterName -or
            $compiledTemplate.parameters.$secureParameterName.type -ne 'secureString') {
            throw "Bicep parameter '$secureParameterName' is not a secureString. No password has been requested and no deployment was submitted."
        }
    }
    foreach ($parameterName in @('imageTag', 'revisionSuffix')) {
        if (-not $compiledTemplate.parameters.$parameterName) {
            throw "Required Bicep parameter '$parameterName' was not found. No password has been requested and no deployment was submitted."
        }
    }
    $bicepSource = [System.IO.File]::ReadAllText($templateFile)
    if ($bicepSource -notmatch "name:\s*'acerportfoliobxwiyolb'\s*\r?\n\s*scope:\s*resourceGroup\('rg-er-portfolio-demo-eastus2'\)" -or
        $bicepSource -notmatch "name:\s*'id-er-portfolio-api'" -or
        $bicepSource -notmatch "'portfolio-20261024-200121-sqlclient711'" -or
        $bicepSource -notmatch "server:\s*registry\.properties\.loginServer\s+identity:\s*apiIdentity\.id" -or
        $bicepSource -notmatch "image:\s*'\$\{registry\.properties\.loginServer\}/emergencyresponse-api:\$\{imageTag\}'" -or
        $bicepSource -notmatch "'AI__Enabled',\s*value:\s*'false'") {
        throw 'The Bicep template does not bind the expected East US 2 registry, immutable API image, and API managed-identity pull configuration. No password has been requested and no deployment was submitted.'
    }
    $bicepSource = $null

    if ([Console]::IsInputRedirected -or [Console]::IsOutputRedirected) {
        throw 'Run this script directly in an interactive PowerShell terminal. No password has been requested and no deployment was submitted.'
    }

    if ($PreflightOnly) {
        Write-Host 'Preflight-only mode: creating disposable secure parameter values in memory; no real password will be requested.' -ForegroundColor Cyan
        $adminPasswordSecure = New-RandomSecureString -ByteCount 24 -Suffix 'Aa1!'
        $jwtSigningKeySecure = New-RandomSecureString -ByteCount 32
    } else {
        Write-Host ''
        Write-Host 'The next prompt is MASKED: PowerShell will hide every character you type.' -ForegroundColor Yellow
        Write-Host 'The seeder uses this password only if the demo admin does not already exist; it does not change an existing account password.'
        Write-Host 'Use a NEW demo admin password of at least 12 characters, with uppercase, lowercase, a number, and a symbol.'
        try {
            $adminPasswordSecure = Read-Host -Prompt 'Enter a NEW demo admin password (input hidden)' -AsSecureString
        } catch {
            throw 'Secure password input failed. No deployment was submitted.'
        }
        if (-not $adminPasswordSecure -or $adminPasswordSecure.Length -lt 12) {
            throw 'The masked password input was empty or shorter than the template minimum. No deployment was submitted.'
        }
        $jwtSigningKeySecure = New-RandomSecureString -ByteCount 32
    }
    if (-not $jwtSigningKeySecure -or $jwtSigningKeySecure.Length -lt 32) {
        throw 'Could not prepare the required secure JWT signing parameter. No deployment was submitted.'
    }

    $utcStamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
    $deploymentName = "portfolio-api-native-secrets-$utcStamp"
    $revisionSuffix = 'native-' + [DateTime]::UtcNow.ToString('yyMMddHHmmss')
    $parameterObject = @{
        imageTag             = $imageTag
        revisionSuffix       = $revisionSuffix
        apiJwtSigningKey     = $jwtSigningKeySecure
        apiAdminSeedPassword = $adminPasswordSecure
    }

    Write-Host ''
    Write-Host 'Running a fresh API-only what-if. Secret values are withheld from the displayed changes.' -ForegroundColor Cyan
    $changeRows = @(Invoke-VerifiedApiWhatIf `
        -DeploymentName $deploymentName `
        -Parameters $parameterObject `
        -TemplateObject $compiledTemplate `
        -SubscriptionId $subscription.Id `
        -ExpectedApiResourceId $appResourceId)

    if ($PreflightOnly) {
        Write-Host 'PREFLIGHT PASSED. No password was requested and no deployment was submitted.' -ForegroundColor Green
        return
    }

    Write-Host ''
    Write-Host 'WHAT-IF GATE PASSED: exactly one API app modification; no unrelated actionable changes.' -ForegroundColor Green
    Write-Host 'This is the separate deployment confirmation. Type DEPLOY exactly to submit the API-only incremental deployment.' -ForegroundColor Yellow
    $confirmation = Read-Host -Prompt 'Type DEPLOY to continue'
    if ($confirmation -cne 'DEPLOY') {
        Write-Host 'Confirmation was not DEPLOY. Aborting without deployment.' -ForegroundColor Yellow
        return
    }

    Write-Host ("Submitting API deployment to ARM: {0}" -f $deploymentName) -ForegroundColor Cyan
    $deployment = Invoke-ApiResourceGroupDeployment `
        -DeploymentName $deploymentName `
        -Parameters $parameterObject `
        -TemplateObject $compiledTemplate `
        -SubscriptionId $subscription.Id
    if ([string]$deployment.ProvisioningState -ne 'Succeeded') {
        throw 'ARM did not confirm a successful API deployment. Do not resubmit; inspect the deployment record.'
    }
    Write-Host ("Deployment succeeded: {0}" -f $deployment.DeploymentName) -ForegroundColor Green
    Write-Host ("Provisioning state: {0}" -f $deployment.ProvisioningState)
    if ($deployment.CorrelationId) { Write-Host ("Correlation ID: {0}" -f $deployment.CorrelationId) }
} finally {
    if ($parameterObject) { $parameterObject.Clear() }
    $compiledTemplateText = $null
    $compiledTemplate = $null
    if ($adminPasswordSecure) { $adminPasswordSecure.Dispose() }
    if ($jwtSigningKeySecure) { $jwtSigningKeySecure.Dispose() }
}

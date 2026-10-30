#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('api', 'web')][string]$Component,
    [Parameter(Mandatory)][ValidatePattern('^acerportfoliobxwiyolb\.azurecr\.io/emergencyresponse-(api|web)@sha256:[a-f0-9]{64}$')][string]$Image,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$')][string]$RevisionSuffix
)

# Read-only, interactive before/after guard. Run this in one terminal, perform the
# separately approved `az containerapp update` in another, then type COMPARE.
# It retains the baseline in memory only. It never writes snapshots or prints env
# values, secret values, request bodies, tokens, or raw Azure responses.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$subscription = '0a05d5ff-7fef-4886-8e72-22a6cafd5567'
$resourceGroup = 'rg-er-portfolio-demo-centralus'
$environmentName = 'cae-er-portfolio-demo'
$appName = "ca-er-$Component-portfolio"
$containerName = $Component
$origin = 'https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io'
$oldImages = @{
    # ACA reports the deployed API image by its configured immutable release tag.
    # The tag's ACR digest is checked separately below before accepting it as rollback.
    api = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api:portfolio-20261024-200121-sqlclient711'
    web = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b'
}
$oldImageDigests = @{ api = 'sha256:c09b299562f3eaae4786b2361f73e9a214c157f3d6d863b6ca1858e070a8cb4a' }
$identityNames = @{ api = 'id-er-portfolio-api'; web = 'id-er-portfolio-web' }
$secretNames = @{ api = @('admin-seed-password', 'jwt-signing-key'); web = @('syncfusion-license-key') }
$expectedRegistry = 'acerportfoliobxwiyolb.azurecr.io'

function Invoke-AzJson([string[]]$Arguments) {
    $output = & az @Arguments 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $output) {
        throw 'Read-only Azure snapshot request failed; comparison stopped.'
    }
    try { return (($output -join "`n") | ConvertFrom-Json -AsHashtable -ErrorAction Stop) }
    catch { throw 'Azure snapshot response was not valid JSON; comparison stopped.' }
}

function Convert-ComparableJson([object]$Value) {
    return ConvertTo-Json -InputObject $Value -Depth 100 -Compress
}

function ConvertTo-SafeComparable([object]$Value, [string]$PropertyName = '', [string]$ParentPath = '') {
    # Sanitize secret-bearing fields before any serialization/comparison. Values
    # remain in process memory only and are never included in output or errors.
    $credentialField = $PropertyName -match '(?i)^(password|token|clientSecret|secretValue|accessKey|connectionString|licenseKey)$'
    $secretValueField = $PropertyName -ceq 'value' -and $ParentPath -match '(?i)(^|\.)secrets($|\.)'
    if ($credentialField -or $secretValueField) {
        if ($null -eq $Value -or [string]::IsNullOrEmpty([string]$Value)) { return $null }
        return '<redacted-present>'
    }
    if ($Value -is [System.Collections.IDictionary]) {
        $safe = [ordered]@{}
        foreach ($key in @($Value.Keys | Sort-Object)) {
            $safe[[string]$key] = ConvertTo-SafeComparable $Value[$key] ([string]$key) $(if ($ParentPath) { "$ParentPath.$key" } else { [string]$key })
        }
        return $safe
    }
    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string]) {
        $items = @()
        foreach ($item in $Value) { $items += ,(ConvertTo-SafeComparable $item $PropertyName $ParentPath) }
        return ,$items
    }
    return $Value
}

function Assert-RevisionSuffix([string]$Expected, [string]$AppName, [string]$CurrentRevision, [string[]]$ExistingRevisionNames) {
    if ([string]::IsNullOrWhiteSpace($Expected) -or $Expected -notmatch '^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$') {
        throw 'Expected revision suffix is missing or invalid; comparison stopped.'
    }
    $revisionName = "$AppName--$Expected"
    if ($CurrentRevision -eq $revisionName -or @($ExistingRevisionNames | Where-Object { $_ -ceq $revisionName }).Count -gt 0) {
        throw 'Expected revision suffix is already in use; comparison stopped.'
    }
}

function Assert-ObservedRevisionSuffix([object]$Template, [string]$Expected) {
    if ($Template -isnot [System.Collections.IDictionary] -or
        -not $Template.Contains('revisionSuffix') -or
        [string]$Template['revisionSuffix'] -cne $Expected) {
        throw 'Observed revision suffix did not match the approved expected suffix; comparison stopped.'
    }
}

function Get-EnvMap([object]$Template, [string]$ExpectedContainerName) {
    if (@($Template.containers).Count -ne 1 -or $Template.containers[0].name -ne $ExpectedContainerName) {
        throw 'Unexpected container inventory; comparison stopped.'
    }
    $map = @{}
    foreach ($item in @($Template.containers[0].env)) {
        if ($item -isnot [System.Collections.IDictionary]) {
            throw 'Environment entry has an unsupported structure; comparison stopped.'
        }
        $allowedFields = @('name', 'value', 'secretRef')
        if (@($item.Keys | Where-Object { $_ -notin $allowedFields }).Count -gt 0) {
            throw 'Environment entry contains an unapproved field; comparison stopped.'
        }
        $hasValue = $item.Contains('value')
        $hasSecretRef = $item.Contains('secretRef')
        $value = if ($hasValue) { $item['value'] } else { $null }
        $secretRef = if ($hasSecretRef) { $item['secretRef'] } else { $null }
        if (($hasValue -and $null -ne $value -and $value -isnot [string]) -or
            ($hasSecretRef -and $null -ne $secretRef -and $secretRef -isnot [string])) {
            throw 'Environment entry has an invalid field type; comparison stopped.'
        }
        if ([string]::IsNullOrWhiteSpace([string]$item['name']) -or $map.ContainsKey([string]$item['name'])) {
            throw 'Missing or duplicate environment variable name; comparison stopped.'
        }
        $hasUsableValue = $hasValue -and $null -ne $value
        $hasUsableSecretRef = $hasSecretRef -and $null -ne $secretRef
        if ($hasUsableValue -eq $hasUsableSecretRef) {
            throw 'Environment entry must contain exactly one non-null value or secret reference; comparison stopped.'
        }
        # Values stay in this process only and are never serialized or emitted.
        $map[[string]$item['name']] = @{
            hasValue = $hasValue
            value = $value
            hasSecretRef = $hasSecretRef
            secretRef = $secretRef
        }
    }
    return ,$map
}

function Get-Snapshot {
    $appQuery = '{location:location,identity:identity,properties:{provisioningState:properties.provisioningState,environmentId:properties.environmentId,latestRevisionName:properties.latestRevisionName,latestReadyRevisionName:properties.latestReadyRevisionName,configuration:properties.configuration,template:properties.template}}'
    $app = Invoke-AzJson @('containerapp','show','--subscription',$subscription,'--resource-group',$resourceGroup,'--name',$appName,'--query',$appQuery,'--output','json','--only-show-errors')
    $environmentQuery = '{location:location,provisioningState:properties.provisioningState,vnetConfiguration:properties.vnetConfiguration,publicNetworkAccess:properties.publicNetworkAccess,zoneRedundant:properties.zoneRedundant,workloadProfiles:properties.workloadProfiles,staticIp:properties.staticIp,peerAuthentication:properties.peerAuthentication,peerTrafficEncryption:properties.peerTrafficEncryption}'
    $environment = Invoke-AzJson @('containerapp','env','show','--subscription',$subscription,'--resource-group',$resourceGroup,'--name',$environmentName,'--query',$environmentQuery,'--output','json','--only-show-errors')
    $revisionsQuery = '[].{name:name,active:properties.active,traffic:properties.trafficWeight,health:properties.healthState,state:properties.provisioningState,image:properties.template.containers[0].image}'
    $allRevisions = @(Invoke-AzJson @('containerapp','revision','list','--subscription',$subscription,'--resource-group',$resourceGroup,'--name',$appName,'--query',$revisionsQuery,'--output','json','--only-show-errors'))
    $revisions = @($allRevisions | Where-Object { $_.active -eq $true })

    if ($app.location -replace '\s','' -ine 'centralus' -or
        $app.properties.environmentId -notmatch '/managedEnvironments/cae-er-portfolio-demo$' -or
        $app.properties.provisioningState -ne 'Succeeded' -or
        $app.properties.configuration.activeRevisionsMode -ne 'Single' -or
        -not $app.properties.latestReadyRevisionName -or
        $app.properties.latestRevisionName -ne $app.properties.latestReadyRevisionName -or
        $environment.location -replace '\s','' -ine 'centralus' -or
        $environment.provisioningState -ne 'Succeeded') {
        throw 'Location, environment, mode, or ready-revision baseline mismatch; comparison stopped.'
    }
    if ($revisions.Count -ne 1 -or $revisions[0].name -ne $app.properties.latestReadyRevisionName -or
        $revisions[0].traffic -ne 100 -or $revisions[0].health -ne 'Healthy' -or
        $revisions[0].state -ne 'Provisioned') {
        throw 'Expected one healthy, provisioned revision with 100% traffic; comparison stopped.'
    }
    $observedSecretNames = @($app.properties.configuration.secrets | ForEach-Object { $_.name } | Sort-Object)
    if ($observedSecretNames -join '|' -cne (@($secretNames[$Component] | Sort-Object) -join '|')) {
        throw 'Native secret-name inventory mismatch; comparison stopped.'
    }
    if (@($app.identity.userAssignedIdentities.Keys | Where-Object { $_ -match "/$($identityNames[$Component])$" }).Count -ne 1) {
        throw 'Expected user-assigned identity is missing; comparison stopped.'
    }
    if (@($app.properties.configuration.registries | Where-Object { $_.server -eq $expectedRegistry -and $_.identity -match "/$($identityNames[$Component])$" }).Count -ne 1) {
        throw 'Expected managed-identity registry configuration is missing; comparison stopped.'
    }
    if ($revisions[0].image -ne $app.properties.template.containers[0].image) {
        throw 'Revision and app image references disagree; comparison stopped.'
    }

    # Normalize the full app configuration before comparing it. This catches
    # unknown configuration drift while reducing secret values to presence only.
    $app.properties.configuration = ConvertTo-SafeComparable $app.properties.configuration
    return @{
        app = $app
        environment = $environment
        revisions = $revisions
        allRevisionNames = @($allRevisions | ForEach-Object { [string]$_.name })
        env = Get-EnvMap $app.properties.template $containerName
    }
}

function Assert-Same([string]$Label, [object]$Before, [object]$After, [System.Collections.Generic.List[string]]$Failures) {
    if ((Convert-ComparableJson $Before) -cne (Convert-ComparableJson $After)) { $Failures.Add($Label) }
}

if ($Image -notmatch "/emergencyresponse-$Component@") { throw 'Image/component mismatch; comparison stopped.' }
$context = Invoke-AzJson @('account','show','--subscription',$subscription,'--query','{id:id,name:name,state:state}','--output','json','--only-show-errors')
if ($context.id -ne $subscription -or $context.name -ne 'Azure subscription 1' -or $context.state -ne 'Enabled') {
    throw 'Azure subscription mismatch; comparison stopped.'
}

$before = Get-Snapshot
if ($before.revisions[0].image -ne $oldImages[$Component]) { throw 'Healthy rollback image differs from the reviewed baseline; comparison stopped.' }
if ($oldImageDigests.ContainsKey($Component)) {
    $imageName = 'emergencyresponse-api:portfolio-20261024-200121-sqlclient711'
    $observedRollbackDigest = Invoke-AzJson @('acr','repository','show','--name','acerportfoliobxwiyolb','--image',$imageName,'--query','digest','--output','json','--only-show-errors')
    if ($observedRollbackDigest -cne $oldImageDigests[$Component]) {
        throw 'Rollback image tag no longer resolves to its reviewed digest; comparison stopped.'
    }
}
Assert-RevisionSuffix $RevisionSuffix $appName $before.app.properties.latestRevisionName $before.allRevisionNames
$approved = if ($Component -eq 'api') {
    @{
        'AI__Enabled' = 'false'
        'Forecasting__BackgroundWorkerEnabled' = 'false'
        'DataRetention__BackgroundWorkerEnabled' = 'false'
        'AzureMaps__Enabled' = 'false'
        'Cors__AllowedOrigins__0' = $origin
    }
} else { @{} }
if ($Component -eq 'api') {
    if ($before.env['AI__Enabled'].value -cne 'false') { throw 'AI-disabled baseline mismatch; comparison stopped.' }
    if (@($before.env.Keys | Where-Object { $_ -match '^(AzureOpenAI|Foundry|AI__)' -and $_ -ne 'AI__Enabled' }).Count) {
        throw 'Unexpected AI endpoint/model environment setting; comparison stopped.'
    }
}
foreach ($name in @($approved.Keys)) {
    if ($before.env.ContainsKey($name) -and $name -ne 'AI__Enabled') { throw 'Portfolio setting already exists; review drift before continuing.' }
}

Write-Host "Captured sanitized $Component baseline in memory. No snapshot file was written."
Write-Host ("Current ready revision: {0}; current image: {1}" -f $before.revisions[0].name, $before.revisions[0].image)
Write-Host ("Secret names: {0}; environment names: {1}" -f (@($secretNames[$Component]) -join ', '), (@($before.env.Keys | Sort-Object) -join ', '))
Write-Host 'Perform only the separately reviewed `az containerapp update` in another terminal. Return here and type COMPARE.'
$confirmation = Read-Host 'Type COMPARE after that update has completed'
if ($confirmation -cne 'COMPARE') { throw 'Comparison not authorized; no Azure update was issued by this script.' }

$after = Get-Snapshot
$failures = [System.Collections.Generic.List[string]]::new()
if ($after.revisions[0].image -ne $Image) { $failures.Add('revision image') }
try { Assert-ObservedRevisionSuffix $after.app.properties.template $RevisionSuffix }
catch { $failures.Add('revision suffix') }
Assert-Same 'app location' $before.app.location $after.app.location $failures
Assert-Same 'managed environment ID' $before.app.properties.environmentId $after.app.properties.environmentId $failures
Assert-Same 'managed identities' $before.app.identity $after.app.identity $failures
Assert-Same 'complete sanitized app configuration' $before.app.properties.configuration $after.app.properties.configuration $failures
Assert-Same 'environment networking' $before.environment $after.environment $failures

$beforeTemplate = ConvertTo-SafeComparable $before.app.properties.template | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
$afterTemplate = ConvertTo-SafeComparable $after.app.properties.template | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
[void]$beforeTemplate.Remove('revisionSuffix'); [void]$afterTemplate.Remove('revisionSuffix')
foreach ($template in @($beforeTemplate, $afterTemplate)) {
    if (@($template.containers).Count -ne 1) { throw 'Unexpected post-update container inventory; comparison stopped.' }
    [void]$template.containers[0].Remove('image')
    [void]$template.containers[0].Remove('env')
}
Assert-Same 'container probes/resources/scale/volumes' $beforeTemplate $afterTemplate $failures

$names = @($before.env.Keys + $after.env.Keys | Sort-Object -Unique)
$changedEnvNames = [System.Collections.Generic.List[string]]::new()
foreach ($name in $names) {
    $old = $before.env[$name]; $new = $after.env[$name]
    if ($null -eq $old -or $null -eq $new) {
        if (-not $approved.ContainsKey($name) -or $null -eq $new) { $failures.Add("environment variable inventory: $name") }
        elseif ($new.hasSecretRef -or $new.value -cne $approved[$name]) { $failures.Add("environment variable binding/value: $name") }
        else { $changedEnvNames.Add($name) }
        continue
    }
    if ($old.hasSecretRef -ne $new.hasSecretRef -or $old.secretRef -cne $new.secretRef) { $failures.Add("environment secret reference: $name"); continue }
    if ($old.hasValue -ne $new.hasValue) { $failures.Add("environment value presence: $name"); continue }
    if ($old.value -cne $new.value) {
        if (-not $approved.ContainsKey($name) -or $new.value -cne $approved[$name] -or $new.hasSecretRef) { $failures.Add("environment variable value: $name") }
        else { $changedEnvNames.Add($name) }
    }
}

if ($failures.Count) {
    Write-Host 'FAIL-CLOSED. Unexpected differences (values withheld):'
    $failures | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    throw 'Post-update configuration comparison failed. Stop rollout and review read-only state.'
}
Write-Host ("PASS: only intended image{0} changed." -f $(if ($changedEnvNames.Count) { ' and approved API environment settings' } else { '' }))
if ($changedEnvNames.Count) { Write-Host ("Changed environment variable names: {0}" -f (@($changedEnvNames | Sort-Object -Unique) -join ', ')) }
Write-Host ("PASS: approved revision suffix {0}; secrets/references, identity, registry, ingress/traffic, scale, probes, networking, and all other environment entries are unchanged." -f $RevisionSuffix)

#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('api', 'web')][string]$Component,
    [Parameter(Mandatory)][ValidatePattern('^acerportfoliobxwiyolb\.azurecr\.io/emergencyresponse-(api|web)@sha256:[a-f0-9]{64}$')][string]$Image
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
    api = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-api@sha256:c09b299562f3eaae4786b2361f73e9a214c157f3d6d863b6ca1858e070a8cb4a'
    web = 'acerportfoliobxwiyolb.azurecr.io/emergencyresponse-web@sha256:3593c2c7f55cac72ea9912b533edbf06891c90f85b58948f7567d87c51a9926b'
}
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

function Get-EnvMap([object]$Template) {
    if (@($Template.containers).Count -ne 1 -or $Template.containers[0].name -ne $containerName) {
        throw 'Unexpected container inventory; comparison stopped.'
    }
    $map = @{}
    foreach ($item in @($Template.containers[0].env)) {
        if (-not $item.name -or $map.ContainsKey([string]$item.name)) {
            throw 'Missing or duplicate environment variable name; comparison stopped.'
        }
        # Values stay in this process only and are never serialized or emitted.
        $map[[string]$item.name] = @{ value = $item.value; secretRef = $item.secretRef }
    }
    return ,$map
}

function Get-Snapshot {
    $appQuery = '{location:location,identity:identity,properties:{provisioningState:properties.provisioningState,environmentId:properties.environmentId,latestRevisionName:properties.latestRevisionName,latestReadyRevisionName:properties.latestReadyRevisionName,configuration:{activeRevisionsMode:properties.configuration.activeRevisionsMode,secrets:properties.configuration.secrets[].{name:name,keyVaultUrl:keyVaultUrl,identity:identity},registries:properties.configuration.registries[].{server:server,identity:identity,username:username,passwordSecretRef:passwordSecretRef},ingress:properties.configuration.ingress},template:properties.template}}'
    $app = Invoke-AzJson @('containerapp','show','--subscription',$subscription,'--resource-group',$resourceGroup,'--name',$appName,'--query',$appQuery,'--output','json','--only-show-errors')
    $environmentQuery = '{location:location,provisioningState:properties.provisioningState,internal:properties.vnetConfiguration.internal,infrastructureSubnetId:properties.vnetConfiguration.infrastructureSubnetId,publicNetworkAccess:properties.publicNetworkAccess}'
    $environment = Invoke-AzJson @('containerapp','env','show','--subscription',$subscription,'--resource-group',$resourceGroup,'--name',$environmentName,'--query',$environmentQuery,'--output','json','--only-show-errors')
    $revisionsQuery = '[?properties.active].{name:name,traffic:properties.trafficWeight,health:properties.healthState,state:properties.provisioningState,image:properties.template.containers[0].image}'
    $revisions = @(Invoke-AzJson @('containerapp','revision','list','--subscription',$subscription,'--resource-group',$resourceGroup,'--name',$appName,'--query',$revisionsQuery,'--output','json','--only-show-errors'))

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

    return @{
        app = $app
        environment = $environment
        revisions = $revisions
        env = Get-EnvMap $app.properties.template
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
Assert-Same 'app location' $before.app.location $after.app.location $failures
Assert-Same 'managed environment ID' $before.app.properties.environmentId $after.app.properties.environmentId $failures
Assert-Same 'managed identities' $before.app.identity $after.app.identity $failures
Assert-Same 'revision mode' $before.app.properties.configuration.activeRevisionsMode $after.app.properties.configuration.activeRevisionsMode $failures
Assert-Same 'native secret names and safe reference metadata' $before.app.properties.configuration.secrets $after.app.properties.configuration.secrets $failures
Assert-Same 'registry configuration' $before.app.properties.configuration.registries $after.app.properties.configuration.registries $failures
Assert-Same 'ingress and traffic configuration' $before.app.properties.configuration.ingress $after.app.properties.configuration.ingress $failures
Assert-Same 'environment networking' $before.environment $after.environment $failures

$beforeTemplate = $before.app.properties.template | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
$afterTemplate = $after.app.properties.template | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
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
        elseif ($new.secretRef -or $new.value -cne $approved[$name]) { $failures.Add("environment variable binding/value: $name") }
        else { $changedEnvNames.Add($name) }
        continue
    }
    if ($old.secretRef -cne $new.secretRef) { $failures.Add("environment secret reference: $name"); continue }
    if ($old.value -cne $new.value) {
        if (-not $approved.ContainsKey($name) -or $new.value -cne $approved[$name] -or $new.secretRef) { $failures.Add("environment variable value: $name") }
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
Write-Host 'PASS: secrets/references, identity, registry, ingress/traffic, scale, probes, networking, and all other environment entries are unchanged.'

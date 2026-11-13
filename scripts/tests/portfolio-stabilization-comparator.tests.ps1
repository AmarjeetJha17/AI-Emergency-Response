#requires -Version 7.0
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$comparatorPath = Join-Path $PSScriptRoot '../deployment/compare-portfolio-stabilization-update.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($comparatorPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Comparator syntax validation failed.' }

# Load only pure comparison helpers; the Azure snapshot entry point is not run.
$helperNames = @('Convert-ComparableJson', 'ConvertTo-SafeComparable', 'Assert-RevisionSuffix', 'Assert-ObservedRevisionSuffix', 'Get-EnvMap', 'Assert-SecretReferencesResolve', 'Assert-Same')
foreach ($name in $helperNames) {
    $helperName = $name
    $functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $helperName }, $true)
    if (-not $functionAst) { throw 'A required pure comparator helper was not found.' }
    . ([scriptblock]::Create($functionAst.Extent.Text))
}

function Assert-Rejected([scriptblock]$Action, [string]$Description) {
    $rejected = $false
    try { & $Action } catch { $rejected = $true }
    if (-not $rejected) { throw "Expected rejection: $Description" }
}

Assert-Rejected { Assert-RevisionSuffix '' 'ca-er-api-portfolio' 'ca-er-api-portfolio--old' @('ca-er-api-portfolio--old') } 'missing suffix'
Assert-Rejected { Assert-RevisionSuffix 'used-r1' 'ca-er-api-portfolio' 'ca-er-api-portfolio--old' @('ca-er-api-portfolio--used-r1') } 'suffix already used'
Assert-Rejected { Assert-ObservedRevisionSuffix @{ revisionSuffix = 'different-r2' } 'expected-r1' } 'unexpected observed suffix'
Assert-Rejected { Assert-ObservedRevisionSuffix @{} 'expected-r1' } 'missing observed suffix'

$template = '{"containers":[{"name":"api","env":[{"name":"SETTING","value":"safe"}]}]}' | ConvertFrom-Json -AsHashtable
$envMap = Get-EnvMap $template 'api'
if ($envMap['SETTING'].value -cne 'safe' -or -not $envMap['SETTING'].hasValue -or $envMap['SETTING'].hasSecretRef) {
    throw 'Plain-value environment entry without optional secretRef was not normalized.'
}
$secretTemplate = '{"containers":[{"name":"api","env":[{"name":"SECRET_SETTING","secretRef":"runtime-secret"}]}]}' | ConvertFrom-Json -AsHashtable
$secretMap = Get-EnvMap $secretTemplate 'api'
if ($secretMap['SECRET_SETTING'].secretRef -cne 'runtime-secret' -or $secretMap['SECRET_SETTING'].hasValue -or -not $secretMap['SECRET_SETTING'].hasSecretRef) {
    throw 'secretRef environment entry without optional value was not normalized.'
}
$azureSecretTemplate = '{"containers":[{"name":"api","env":[{"name":"SECRET_SETTING","value":"","secretRef":"runtime-secret"}]}]}' | ConvertFrom-Json -AsHashtable
$azureSecretMap = Get-EnvMap $azureSecretTemplate 'api'
if ($azureSecretMap['SECRET_SETTING'].value -cne '' -or $azureSecretMap['SECRET_SETTING'].secretRef -cne 'runtime-secret') {
    throw 'Azure empty-value plus secretRef representation was not accepted.'
}
Assert-SecretReferencesResolve $azureSecretMap @('runtime-secret')
Assert-Rejected { Assert-SecretReferencesResolve $azureSecretMap @('different-secret') } 'unresolved secret reference'
$emptyLiteralTemplate = '{"containers":[{"name":"api","env":[{"name":"EMPTY_SETTING","value":""}]}]}' | ConvertFrom-Json -AsHashtable
$emptyLiteralMap = Get-EnvMap $emptyLiteralTemplate 'api'
if (-not $emptyLiteralMap['EMPTY_SETTING'].hasValue -or $emptyLiteralMap['EMPTY_SETTING'].value -cne '' -or $emptyLiteralMap['EMPTY_SETTING'].hasSecretRef) {
    throw 'Valid empty literal environment value was not accepted.'
}
$nullTemplate = '{"containers":[{"name":"api","env":[{"name":"SECRET_SETTING","value":null,"secretRef":"runtime-secret"}]}]}' | ConvertFrom-Json -AsHashtable
$nullMap = Get-EnvMap $nullTemplate 'api'
if (-not $nullMap['SECRET_SETTING'].hasValue -or $nullMap['SECRET_SETTING'].value -ne $null -or -not $nullMap['SECRET_SETTING'].hasSecretRef) {
    throw 'Explicit null optional field presence was not preserved.'
}
$absenceDrift = [System.Collections.Generic.List[string]]::new()
Assert-Same 'normalized environment shape' $secretMap['SECRET_SETTING'] $nullMap['SECRET_SETTING'] $absenceDrift
if ($absenceDrift.Count -ne 1) { throw 'Absent and explicit-null properties were not distinguished.' }
Assert-Rejected {
    Get-EnvMap @{ containers = @(@{ name = 'api'; env = @(@{ name = 'SETTING'; value = 'safe'; unexpected = 'field' }) }) } 'api'
} 'unexpected environment-entry field'
Assert-Rejected {
    Get-EnvMap @{ containers = @(@{ name = 'api'; env = @(@{ name = 'SETTING'; value = $null }) }) } 'api'
} 'null-only environment binding'
Assert-Rejected {
    Get-EnvMap ('{"containers":[{"name":"api","env":[{"name":"SETTING"}]}]}' | ConvertFrom-Json -AsHashtable) 'api'
} 'environment entry without a binding'
Assert-Rejected {
    Get-EnvMap ('{"containers":[{"name":"api","env":[{"name":"SETTING","value":"literal","secretRef":"runtime-secret"}]}]}' | ConvertFrom-Json -AsHashtable) 'api'
} 'conflicting literal and secret bindings'

$canary = 'SENSITIVE_DO_NOT_EMIT'
$safe = ConvertTo-SafeComparable @{ secrets = @(@{ name = 'jwt-signing-key'; value = $canary; identity = 'system' }) }
$serializedSafe = Convert-ComparableJson $safe
if ($serializedSafe.Contains($canary)) { throw 'Secret-bearing configuration was serialized without redaction.' }
$failures = [System.Collections.Generic.List[string]]::new()
Assert-Same 'configuration' @{ activeRevisionsMode = 'Single'; ingress = @{ targetPort = 8080 } } @{ activeRevisionsMode = 'Multiple'; ingress = @{ targetPort = 8080 } } $failures
if ($failures.Count -ne 1 -or $failures[0] -cne 'configuration') { throw 'Unauthorized configuration drift was not rejected.' }
Write-Host 'PASS: suffix validation/uniqueness, optional environment fields, absent/null distinction, unexpected-field rejection, redaction and configuration drift. No Azure calls.'

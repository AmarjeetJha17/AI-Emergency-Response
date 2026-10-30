#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$comparatorPath = Join-Path $PSScriptRoot '../deployment/compare-portfolio-stabilization-update.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($comparatorPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Comparator syntax validation failed.' }

# Load only pure comparison helpers; the Azure snapshot entry point is not run.
$helperNames = @('Convert-ComparableJson', 'ConvertTo-SafeComparable', 'Assert-RevisionSuffix', 'Assert-ObservedRevisionSuffix', 'Get-EnvMap', 'Assert-Same')
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

$template = @{ containers = @(@{ name = 'api'; env = @(@{ name = 'SETTING'; value = 'safe' }) }) }
$envMap = Get-EnvMap $template 'api'
if ($envMap['SETTING'].value -cne 'safe') { throw 'Expected env value was not normalized.' }
Assert-Rejected {
    Get-EnvMap @{ containers = @(@{ name = 'api'; env = @(@{ name = 'SETTING'; value = 'safe'; unexpected = 'field' }) }) } 'api'
} 'unexpected environment-entry field'

$canary = 'SENSITIVE_DO_NOT_EMIT'
$safe = ConvertTo-SafeComparable @{ secrets = @(@{ name = 'jwt-signing-key'; value = $canary; identity = 'system' }) }
$serializedSafe = Convert-ComparableJson $safe
if ($serializedSafe.Contains($canary)) { throw 'Secret-bearing configuration was serialized without redaction.' }
$failures = [System.Collections.Generic.List[string]]::new()
Assert-Same 'configuration' @{ activeRevisionsMode = 'Single'; ingress = @{ targetPort = 8080 } } @{ activeRevisionsMode = 'Multiple'; ingress = @{ targetPort = 8080 } } $failures
if ($failures.Count -ne 1 -or $failures[0] -cne 'configuration') { throw 'Unauthorized configuration drift was not rejected.' }
Write-Host 'PASS: suffix validation/uniqueness, environment schema, redaction and configuration drift. No Azure calls.'

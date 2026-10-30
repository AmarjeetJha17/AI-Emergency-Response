#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$path = Join-Path $PSScriptRoot '../deployment/preview-portfolio-stabilization.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Preview script syntax validation failed.' }
$function = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-DeltaPaths' }, $true)
# Load ONLY the pure metadata formatter; do not execute the preview entry point.
. ([scriptblock]::Create($function.Extent.Text))
$canary = 'password=DO_NOT_EMIT;token=DO_NOT_EMIT'
$inputDelta = @(@{ path='properties.template.containers'; propertyChangeType='Array'; before=$canary; after=$canary; children=@(
    @{ path='0'; propertyChangeType='Modify'; children=@(@{path='image';propertyChangeType='Modify';before=$canary;after=$canary}) }
) })
$actual = @(Get-DeltaPaths $inputDelta)
if ($actual.Count -ne 3 -or $actual[2].Path -cne 'properties.template.containers[0].image') { throw 'Nested delta paths are incorrect.' }
$serialized = $actual | ConvertTo-Json
if ($serialized.Contains('DO_NOT_EMIT') -or $serialized.Contains('before') -or $serialized.Contains('after')) { throw 'Delta formatter disclosed payload fields.' }
foreach ($bad in @(
    @{path="properties.template.`nsecret";propertyChangeType='Modify'},
    @{path='properties.template';propertyChangeType=$canary}
)) {
    $rejected = $false
    try { $null = Get-DeltaPaths @($bad) } catch { $rejected = $true }
    if (-not $rejected) { throw 'Invalid metadata was accepted.' }
}
Write-Host 'PASS: syntax, nested paths, before/after redaction and malformed metadata rejection. No Azure calls.'

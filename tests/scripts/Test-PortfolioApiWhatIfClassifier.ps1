$ErrorActionPreference = 'Stop'

$deploymentScript = Resolve-Path (Join-Path $PSScriptRoot '..\..\scripts\deployment\deploy-portfolio-demo-api-native-secrets.ps1')
$tokens = $null
$parseErrors = $null
$scriptAst = [System.Management.Automation.Language.Parser]::ParseFile(
    $deploymentScript.Path,
    [ref]$tokens,
    [ref]$parseErrors
)
if ($parseErrors.Count -gt 0) {
    throw 'The API deployment script has PowerShell parse errors.'
}

$classifierAst = $scriptAst.Find({
    param($node)
    $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -eq 'Test-ExpectedApiWhatIfPropertyPath'
}, $true)
if (-not $classifierAst) {
    throw 'The expected What-If property classifier was not found.'
}

. ([scriptblock]::Create($classifierAst.Extent.Text))

function Assert-Classification {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$ChangeType,
        [Parameter(Mandatory)][bool]$HasChildren,
        [Parameter(Mandatory)][bool]$Expected
    )

    $actual = Test-ExpectedApiWhatIfPropertyPath `
        -Path $Path `
        -PropertyChangeType $ChangeType `
        -HasChildren $HasChildren
    if ($actual -ne $Expected) {
        throw "Unexpected classification for '$Path' with change type '$ChangeType'."
    }
}

Assert-Classification -Path 'properties.runningStatus' -ChangeType 'Delete' -HasChildren $false -Expected $true
Assert-Classification -Path 'properties.runningStatus' -ChangeType 'Modify' -HasChildren $false -Expected $false
Assert-Classification -Path 'properties.runningStatus' -ChangeType 'Delete' -HasChildren $true -Expected $false
Assert-Classification -Path 'properties.runningStatus.extra' -ChangeType 'Delete' -HasChildren $false -Expected $false
Assert-Classification -Path 'properties.configuration.ingress.exposedPort' -ChangeType 'Delete' -HasChildren $false -Expected $false
Assert-Classification -Path 'properties.configuration.secrets[0].value' -ChangeType 'Delete' -HasChildren $false -Expected $false
Assert-Classification -Path 'properties.configuration.secrets[0].keyVaultUrl' -ChangeType 'Delete' -HasChildren $false -Expected $true
Assert-Classification -Path 'properties.template.containers[0].image' -ChangeType 'Delete' -HasChildren $false -Expected $false

Write-Output 'What-If classifier focused checks passed.'

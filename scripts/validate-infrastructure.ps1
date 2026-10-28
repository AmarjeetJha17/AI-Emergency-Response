param(
    [string] $OutputDirectory = "artifacts/infrastructure"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    $templates = @(git ls-files 'infra/*.bicep' 'infra/**/*.bicep')
    if ($LASTEXITCODE -ne 0 -or $templates.Count -eq 0) {
        throw "Could not locate tracked Bicep templates."
    }
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    foreach ($template in $templates) {
        $outputName = ($template -replace '[/\\]', '-') -replace '\.bicep$', '.json'
        $outputPath = Join-Path $OutputDirectory $outputName
        Write-Host "Compiling $template"
        & az bicep build --file $template --outfile $outputPath 2>&1 |
            Tee-Object -FilePath ("$outputPath.log")
        if ($LASTEXITCODE -ne 0) { throw "Bicep compilation failed: $template" }
    }
    Write-Host "Compiled $($templates.Count) templates. No Azure deployment was performed."
}
finally {
    Pop-Location
}

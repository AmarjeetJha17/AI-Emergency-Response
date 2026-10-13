param(
    [Parameter(Mandatory = $true)]
    [string] $ReportPath,

    [ValidateRange(0, 100)]
    [double] $MinimumLineCoverage = 70
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
    throw "Coverage report was not found: $ReportPath"
}

[xml] $coverageDocument = Get-Content -LiteralPath $ReportPath -Raw
$coverageRoot = $coverageDocument.DocumentElement

if ($null -eq $coverageRoot -or $coverageRoot.LocalName -ne "coverage") {
    throw "Coverage report does not have a Cobertura <coverage> root element."
}

$lineRateText = $coverageRoot.GetAttribute("line-rate")
$lineRate = 0.0

if ([string]::IsNullOrWhiteSpace($lineRateText) -or
    -not [double]::TryParse(
        $lineRateText,
        [System.Globalization.NumberStyles]::Float,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [ref] $lineRate)) {
    throw "Coverage report does not contain a valid root line-rate attribute."
}

$percentage = $lineRate * 100
Write-Host ("Application/domain line coverage: {0:N2}%" -f $percentage)
Write-Host ("Required minimum: {0:N2}%" -f $MinimumLineCoverage)

if ($percentage -lt $MinimumLineCoverage) {
    throw "Coverage threshold failed. Add or improve tests; do not exclude uncovered production code to pass the gate."
}

Write-Host "Coverage threshold passed."

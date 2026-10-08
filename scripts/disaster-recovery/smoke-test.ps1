[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][uri]$BaseUrl,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$HealthPath = '/health/ready'
)
$ErrorActionPreference = 'Stop'
if ($BaseUrl.Scheme -ne 'https' -and $BaseUrl.Host -notin @('localhost', '127.0.0.1')) {
    throw 'Smoke tests require HTTPS except for local development.'
}
$uri = [uri]::new($BaseUrl, $HealthPath)
$response = Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec 20
if ($response.StatusCode -ne 200) { throw "Readiness smoke test returned HTTP $($response.StatusCode)." }
Write-Host "Readiness smoke test succeeded: $uri"
